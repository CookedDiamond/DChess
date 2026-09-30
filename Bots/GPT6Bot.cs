using System;
using System.Collections.Generic;
using DChess.BotApi;
using DChess.Chess.Pieces;
using DChess.Chess.Playground;

namespace DChess.Bots {
    // Original, self-contained search for DChess's king-capture rules.
    // The API is read only: all speculative work uses this private 0x88 board.
    public class GPT6Bot : IChessBot {
        public string Name => "GPT6Bot";
        private const int Mate = 30000, MaxPly = 96, Stride = 256, TableMask = (1 << 18) - 1;
        private readonly int[] cells = new int[128], kings = new int[2], counts = new int[16];
        private readonly int[] moveBuffer = new int[MaxPly * Stride], orderBuffer = new int[MaxPly * Stride];
        private readonly int[] killers = new int[MaxPly * 2], history = new int[2 * 128 * 128];
        private readonly ulong[] keys = new ulong[32 * 128], clockKeys = new ulong[101], path = new ulong[MaxPly];
        private readonly int[] middle = new int[32 * 128], ending = new int[32 * 128];
        private readonly Entry[] table = new Entry[TableMask + 1];
        private readonly State[] states = new State[MaxPly];
        private readonly List<ulong> gameHistory = new List<ulong>(512);
        private readonly int[] knightSteps = { 33, 31, 18, 14, -14, -18, -31, -33 };
        private readonly int[] raySteps = { 1, -1, 16, -16, 17, 15, -15, -17 };
        private readonly int[] values = { 0, 100, 325, 345, 515, 990, 20000 };
        private readonly int[] phases = { 0, 0, 1, 1, 2, 4, 0 };
        private BotTimer clock;
        private ulong hash, sideKey, previousPawns;
        private int side, mg, eg, phase, pieceCount, halfmove, rootPly, previousPly = -2, previousCount;
        private int deadline, rootBest, iterationBest, nodes, generation;
        private bool stopped;

        private struct Entry {
            public ulong Key;
            public int Move, Score;
            public short Depth;
            public byte Bound, Age;
        }
        private struct State {
            public ulong Hash;
            public int Moving, Captured, Rook, Halfmove, Mg, Eg, Phase;
        }

        public GPT6Bot() {
            ulong seed = 0xC6A4A7935BD1E995UL;
            for (int i = 0; i < keys.Length; i++) keys[i] = RandomKey(ref seed);
            for (int i = 0; i < clockKeys.Length; i++) clockKeys[i] = RandomKey(ref seed);
            sideKey = RandomKey(ref seed);
            for (int p = 1; p < 32; p++) {
                int type = p & 7;
                if (type == 0 || type == 7) continue;
                int color = (p >> 3) & 1, sign = color == 0 ? 1 : -1;
                for (int s = 0; s < 128; s++) {
                    if ((s & 0x88) != 0) continue;
                    int x = s & 7, rank = color == 0 ? s >> 4 : 7 - (s >> 4);
                    int center = 7 - Math.Abs(2 * x - 7) - Math.Abs(2 * rank - 7);
                    int m = values[type], e = values[type];
                    switch (type) {
                        case 1:
                            m = 100 + rank * 6 + (3 - Math.Abs(x - 3)) * rank * 2;
                            e = 120 + rank * rank * 3;
                            break;
                        case 2:
                            m = 325 + center * 7 + Math.Min(rank, 4) * 4 - (rank == 0 ? 15 : 0);
                            e = 310 + center * 5;
                            break;
                        case 3:
                            m = 345 + center * 3 + Math.Min(rank, 4) * 4 - (rank == 0 ? 10 : 0);
                            e = 335 + center * 4;
                            break;
                        case 4:
                            m = 515 + rank * 2 + (rank == 6 ? 22 : 0);
                            e = 540 + center * 2 + (rank == 6 ? 15 : 0);
                            break;
                        case 5:
                            m = 990 + center * 2 - Math.Max(0, rank - 2) * 3;
                            e = 970 + center * 3;
                            break;
                        case 6:
                            m = -rank * 14 - Math.Max(0, 3 - Math.Abs(x - 3)) * 8;
                            if (rank == 0 && (x == 1 || x == 2 || x == 6)) m += 35;
                            e = center * 9;
                            break;
                    }
                    middle[p * 128 + s] = sign * m;
                    ending[p * 128 + s] = sign * e;
                }
            }
        }

        private ulong RandomKey(ref ulong seed) {
            seed ^= seed << 13;
            seed ^= seed >> 7;
            seed ^= seed << 17;
            return seed;
        }

        public Move Think(BotBoard board, BotTimer timer) {
            List<Move> legal = board.GetLegalMoves();
            // The arena never calls Think on an empty move list.
            Move fallback = legal[0];
            foreach (Move move in legal)
                if (move.CapturedPiece.Type == PieceType.King) return move;
            clock = timer;
            // A large reserve covers cold JIT, GC, arena scheduling and validation.
            deadline = Math.Max(1, (int)(timer.TimeLimitMilliseconds * 0.70) - 8);
            stopped = false;
            nodes = 0;
            generation++;
            try {
                Initialize(board);
                rootPly = board.PlyCount;
                ulong pawns = PawnKey();
                halfmove = previousPly == rootPly - 1 && previousCount == pieceCount && previousPawns == pawns
                    ? halfmove + 1 : 0;
                if (previousPly != rootPly) gameHistory.Add(hash);
                for (int i = 0; i < history.Length; i++) history[i] /= 2;

                // A completed one-ply pass supplies a safe and useful fallback even
                // if the first deeper iteration runs out of time.
                int count = Generate(0, false), best = -Mate - 1;
                rootBest = 0;
                for (int i = 0; i < count; i++) {
                    if (OutOfTime()) break;
                    int move = moveBuffer[i];
                    if (!MatchesLegal(move, legal)) continue;
                    Make(move, 0);
                    int score = IsDraw(1) ? 0 : Attacked(kings[side ^ 1], side) ? -Mate + 2 : -Evaluate();
                    Undo(move, 0);
                    if (score > best) { best = score; rootBest = move; }
                }
                Move matched = MatchMove(rootBest, legal);
                if (matched != null) fallback = matched;

                int lastScore = best;
                for (int depth = 1; depth < MaxPly - 8 && !OutOfTime(); depth++) {
                    // Do not start a much larger iteration when most of the budget is gone.
                    if (depth > 2 && clock.ElapsedMilliseconds > deadline * 0.74) break;
                    int window = depth >= 4 ? 35 : Mate + 1;
                    int alpha = Math.Max(-Mate, lastScore - window), beta = Math.Min(Mate, lastScore + window);
                    int score;
                    while (true) {
                        iterationBest = rootBest;
                        score = Search(depth, 0, alpha, beta, true, false);
                        if (stopped) break;
                        if (score > alpha && score < beta) break;
                        window *= 3;
                        alpha = Math.Max(-Mate, score - window);
                        beta = Math.Min(Mate, score + window);
                        if (alpha == -Mate && beta == Mate) {
                            score = Search(depth, 0, alpha, beta, true, false);
                            break;
                        }
                    }
                    if (stopped) break;
                    matched = MatchMove(iterationBest, legal);
                    if (matched != null) { rootBest = iterationBest; fallback = matched; }
                    lastScore = score;
                    if (Math.Abs(score) > Mate - MaxPly) break;
                }
                // Remember both our resulting position and the next opponent position.
                int chosen = EncodeLegal(fallback);
                Make(chosen, 0);
                gameHistory.Add(hash);
                previousPawns = PawnKey();
                previousCount = pieceCount;
                previousPly = rootPly + 1;
                int rememberedClock = halfmove;
                Undo(chosen, 0);
                halfmove = rememberedClock;
            }
            catch (Exception) {
                // Search state is private. A valid API move survives any search error.
                previousPly = -2;
                gameHistory.Clear();
            }
            return fallback;
        }

        private bool OutOfTime() {
            if (!stopped && (clock.ElapsedMilliseconds >= deadline || clock.IsTimeUp)) stopped = true;
            return stopped;
        }

        private int TypeCode(PieceType type) => type switch {
            PieceType.Pawn => 1, PieceType.Knight => 2, PieceType.Bishop => 3,
            PieceType.Rook => 4, PieceType.Queen => 5, PieceType.King => 6, _ => 0
        };

        private void Initialize(BotBoard board) {
            Array.Clear(cells, 0, cells.Length);
            Array.Clear(counts, 0, counts.Length);
            side = board.IsWhiteToMove ? 0 : 1;
            hash = side == 1 ? sideKey : 0;
            mg = eg = phase = pieceCount = 0;
            kings[0] = kings[1] = -1;
            foreach (PieceOnSquare item in board.GetAllPieces()) {
                int type = TypeCode(item.Type);
                if (type == 0) continue;
                int color = item.Team == TeamType.White ? 0 : 1;
                int p = type | (color << 3);
                if ((type == 4 || type == 6) && item.Piece.MoveCount == 0) p |= 16;
                int s = item.Square.x + item.Square.y * 16;
                cells[s] = p;
                hash ^= keys[p * 128 + s];
                mg += middle[p * 128 + s];
                eg += ending[p * 128 + s];
                phase += phases[type];
                counts[p & 15]++;
                pieceCount++;
                if (type == 6) kings[color] = s;
            }
        }

        private ulong PawnKey() {
            ulong result = 0;
            for (int s = 0; s < 128; s++) {
                if ((s & 0x88) != 0) { s += 7; continue; }
                if ((cells[s] & 7) == 1) result ^= keys[cells[s] * 128 + s];
            }
            return result;
        }

        // Bits 0..6: from; 7..13: to; 14..21: castling rook's square + 1.
        private int EncodeLegal(Move move) {
            int from = move.From.x + 16 * move.From.y, to = move.To.x + 16 * move.To.y;
            int encoded = from | (to << 7);
            if ((cells[from] & 7) == 6 && Math.Abs(to - from) == 2) {
                int step = to > from ? 1 : -1, rook = from + step;
                while ((rook & 0x88) == 0 && cells[rook] == 0) rook += step;
                if ((rook & 0x88) == 0) encoded |= (rook + 1) << 14;
            }
            return encoded;
        }

        private bool MatchesLegal(int encoded, List<Move> legal) => MatchMove(encoded, legal) != null;
        private Move MatchMove(int encoded, List<Move> legal) {
            int from = encoded & 127, to = (encoded >> 7) & 127;
            foreach (Move move in legal)
                if (move.From.x + 16 * move.From.y == from && move.To.x + 16 * move.To.y == to) return move;
            return null;
        }

        private int Generate(int ply, bool capturesOnly) {
            int start = ply * Stride, n = 0;
            for (int from = 0; from < 128; from++) {
                if ((from & 0x88) != 0) { from += 7; continue; }
                int piece = cells[from], type = piece & 7;
                if (piece == 0 || ((piece >> 3) & 1) != side) continue;
                if (type == 1) {
                    int step = side == 0 ? 16 : -16, to = from + step;
                    if ((to & 0x88) == 0 && cells[to] == 0) {
                        if (!capturesOnly || (to >> 4) == (side == 0 ? 7 : 0)) moveBuffer[start + n++] = from | (to << 7);
                        int twice = to + step;
                        if (!capturesOnly && (from >> 4) == (side == 0 ? 1 : 6) && cells[twice] == 0)
                            moveBuffer[start + n++] = from | (twice << 7);
                    }
                    for (int d = -1; d <= 1; d += 2) {
                        to = from + step + d;
                        if ((to & 0x88) == 0 && cells[to] != 0 && ((cells[to] >> 3) & 1) != side)
                            moveBuffer[start + n++] = from | (to << 7);
                    }
                    continue;
                }
                int first = type == 3 ? 4 : 0, end = type == 4 ? 4 : 8;
                for (int d = first; d < end; d++) {
                    int step = type == 2 ? knightSteps[d] : raySteps[d];
                    int to = from + step;
                    while ((to & 0x88) == 0) {
                        int target = cells[to];
                        if (target != 0 && ((target >> 3) & 1) == side) break;
                        if (!capturesOnly || target != 0) moveBuffer[start + n++] = from | (to << 7);
                        if (target != 0 || type == 2 || type == 6) break;
                        to += step;
                    }
                }
                if (type == 6 && (piece & 16) != 0 && !capturesOnly) {
                    for (int step = -1; step <= 1; step += 2) {
                        int rook = from + step;
                        while ((rook & 0x88) == 0 && cells[rook] == 0) rook += step;
                        if ((rook & 0x88) == 0 && Math.Abs(rook - from) > 2 && cells[rook] == (4 | (side << 3) | 16))
                            moveBuffer[start + n++] = from | ((from + 2 * step) << 7) | ((rook + 1) << 14);
                    }
                }
            }
            return n;
        }

        private void Remove(int square, int piece) {
            hash ^= keys[piece * 128 + square];
            mg -= middle[piece * 128 + square];
            eg -= ending[piece * 128 + square];
        }
        private void Put(int square, int piece) {
            cells[square] = piece;
            hash ^= keys[piece * 128 + square];
            mg += middle[piece * 128 + square];
            eg += ending[piece * 128 + square];
        }

        private void Make(int move, int ply) {
            int from = move & 127, to = (move >> 7) & 127;
            int piece = cells[from], target = cells[to], type = piece & 7;
            states[ply] = new State { Hash = hash, Moving = piece, Captured = target, Halfmove = halfmove, Mg = mg, Eg = eg, Phase = phase };
            path[ply] = hash;
            Remove(from, piece);
            cells[from] = 0;
            if (target != 0) {
                Remove(to, target);
                counts[target & 15]--;
                pieceCount--;
                phase -= phases[target & 7];
                if ((target & 7) == 6) kings[side ^ 1] = -1;
            }
            int placed = piece & 15;
            if (type == 1 && (to >> 4) == (side == 0 ? 7 : 0)) {
                placed = 5 | (side << 3);
                counts[piece & 15]--;
                counts[placed]++;
                phase += 4;
            }
            Put(to, placed);
            if (type == 6) kings[side] = to;
            int rook = (move >> 14) - 1;
            if (rook >= 0) {
                int landing = (from + to) / 2;
                states[ply].Rook = cells[rook];
                Remove(rook, cells[rook]);
                cells[rook] = 0;
                Put(landing, 4 | (side << 3));
            }
            halfmove = type == 1 || target != 0 ? 0 : halfmove + 1;
            side ^= 1;
            hash ^= sideKey;
        }

        private void Undo(int move, int ply) {
            State state = states[ply];
            int from = move & 127, to = (move >> 7) & 127;
            side ^= 1;
            if ((state.Moving & 7) == 1 && (cells[to] & 7) == 5) {
                counts[cells[to] & 15]--;
                counts[state.Moving & 15]++;
            }
            cells[from] = state.Moving;
            cells[to] = state.Captured;
            if (state.Captured != 0) {
                counts[state.Captured & 15]++;
                pieceCount++;
                if ((state.Captured & 7) == 6) kings[side ^ 1] = to;
            }
            if ((state.Moving & 7) == 6) kings[side] = from;
            int rook = (move >> 14) - 1;
            if (rook >= 0) { cells[rook] = state.Rook; cells[(from + to) / 2] = 0; }
            hash = state.Hash;
            mg = state.Mg;
            eg = state.Eg;
            phase = state.Phase;
            halfmove = state.Halfmove;
        }

        private bool Attacked(int square, int color) {
            if (square < 0) return false;
            int pawn = 1 | (color << 3), back = color == 0 ? -16 : 16;
            int a = square + back - 1, b = square + back + 1;
            if ((a & 0x88) == 0 && (cells[a] & 15) == pawn) return true;
            if ((b & 0x88) == 0 && (cells[b] & 15) == pawn) return true;
            for (int d = 0; d < 8; d++) {
                int from = square + knightSteps[d];
                if ((from & 0x88) == 0 && (cells[from] & 15) == (2 | (color << 3))) return true;
                int step = raySteps[d], distance = 1;
                from = square + step;
                while ((from & 0x88) == 0) {
                    int p = cells[from];
                    if (p != 0) {
                        if (((p >> 3) & 1) == color) {
                            int type = p & 7;
                            if (type == 5 || (type == 6 && distance == 1) || type == (d < 4 ? 4 : 3)) return true;
                        }
                        break;
                    }
                    from += step;
                    distance++;
                }
            }
            return false;
        }

        private bool IsDraw(int ply) {
            if (kings[0] < 0 || kings[1] < 0) return false;
            if (halfmove >= 100 || rootPly + ply >= 500) return true;
            if (pieceCount <= 3 && phase <= 1 && counts[1] + counts[9] == 0) return true;
            int repetitions = 1;
            // Root itself is already included in gameHistory; path starts at root.
            for (int i = ply - 2; i >= 1 && i >= ply - halfmove; i -= 2)
                if (path[i] == hash && ++repetitions >= 3) return true;
            int available = Math.Max(0, halfmove - ply);
            for (int i = gameHistory.Count - 1, scanned = 0; i >= 0 && scanned <= available; i--, scanned++)
                if (gameHistory[i] == hash && ++repetitions >= 3) return true;
            return false;
        }

        private int Search(int depth, int ply, int alpha, int beta, bool pv, bool nullBranch) {
            nodes++;
            if ((nodes & 31) == 0 && OutOfTime()) return 0;
            if (kings[side] < 0) return -Mate + ply;
            if (kings[side ^ 1] < 0) return Mate - ply;
            if (!nullBranch && ply > 0 && IsDraw(ply)) return 0;
            if (Attacked(kings[side ^ 1], side)) return Mate - ply - 1;
            if (ply >= MaxPly - 2) return Evaluate();
            bool check = Attacked(kings[side], side ^ 1);
            if (depth <= 0) return Quiescence(ply, alpha, beta, 0, nullBranch);
            if (check && depth <= 8) depth++;
            int originalAlpha = alpha, ttMove = ply == 0 ? rootBest : 0;
            ulong ttKey = hash ^ clockKeys[Math.Min(100, halfmove)];
            ref Entry entry = ref table[(int)ttKey & TableMask];
            if (entry.Key == ttKey) {
                if (entry.Move != 0) ttMove = entry.Move;
                int score = entry.Score > Mate - MaxPly ? entry.Score - ply : entry.Score < -Mate + MaxPly ? entry.Score + ply : entry.Score;
                if (ply > 0 && !pv && entry.Depth >= depth && !nullBranch && halfmove < 90) {
                    if (entry.Bound == 0 || (entry.Bound == 1 && score >= beta) || (entry.Bound == 2 && score <= alpha)) return score;
                }
            }
            int evaluation = check ? -Mate : Evaluate();
            if (!pv && !check && !nullBranch && Math.Abs(beta) < Mate - MaxPly) {
                if (depth <= 5 && evaluation - 95 * depth >= beta) return evaluation;
                if (depth >= 3 && evaluation >= beta && phase > 4 && counts[(side << 3) + 2] + counts[(side << 3) + 3] + counts[(side << 3) + 4] + counts[(side << 3) + 5] > 0) {
                    side ^= 1;
                    hash ^= sideKey;
                    int score = -Search(depth - 3 - depth / 5, ply + 1, -beta, 1 - beta, false, true);
                    side ^= 1;
                    hash ^= sideKey;
                    if (stopped) return 0;
                    if (score >= beta && score < Mate - MaxPly) return score;
                }
            }
            int count = Generate(ply, false), start = ply * Stride;
            if (count == 0) return 0;
            Order(ply, count, ttMove);
            int best = -Mate + ply + 2, bestMove = 0, searched = 0;
            for (int i = 0; i < count; i++) {
                if ((i & 7) == 0 && OutOfTime()) return 0;
                int move = Pick(start, i, count), from = move & 127, to = (move >> 7) & 127;
                bool quiet = cells[to] == 0 && !Promotes(move);
                Make(move, ply);
                bool draw = !nullBranch && IsDraw(ply + 1);
                if (!draw && Attacked(kings[side ^ 1], side)) { Undo(move, ply); continue; }
                bool givesCheck = Attacked(kings[side], side ^ 1);
                int score;
                if (draw) score = 0;
                else if (searched == 0) score = -Search(depth - 1, ply + 1, -beta, -alpha, pv, nullBranch);
                else {
                    int reduction = !check && !givesCheck && quiet && depth >= 3 && searched >= 4
                        ? 1 + (depth >= 7 && searched >= 10 ? 1 : 0) : 0;
                    score = -Search(depth - 1 - reduction, ply + 1, -alpha - 1, -alpha, false, nullBranch);
                    if (score > alpha && reduction != 0) score = -Search(depth - 1, ply + 1, -alpha - 1, -alpha, false, nullBranch);
                    if (score > alpha && score < beta && pv) score = -Search(depth - 1, ply + 1, -beta, -alpha, true, nullBranch);
                }
                Undo(move, ply);
                if (stopped) return 0;
                searched++;
                if (score > best) { best = score; bestMove = move; }
                if (score > alpha) {
                    alpha = score;
                    if (ply == 0) iterationBest = move;
                    if (alpha >= beta) {
                        if (quiet) {
                            if (killers[ply * 2] != move) { killers[ply * 2 + 1] = killers[ply * 2]; killers[ply * 2] = move; }
                            int index = (side * 128 + from) * 128 + to;
                            int bonus = Math.Min(1600, depth * depth * 20);
                            history[index] += bonus - history[index] * bonus / 16000;
                        }
                        break;
                    }
                }
            }
            if (!nullBranch && !stopped && (entry.Key == ttKey || entry.Age != (byte)generation || depth >= entry.Depth - 2)) {
                entry = new Entry {
                    Key = ttKey, Move = bestMove, Score = best > Mate - MaxPly ? best + ply : best < -Mate + MaxPly ? best - ply : best,
                    Depth = (short)depth, Bound = (byte)(best >= beta ? 1 : best <= originalAlpha ? 2 : 0), Age = (byte)generation
                };
            }
            return best;
        }

        private int Quiescence(int ply, int alpha, int beta, int qdepth, bool nullBranch) {
            nodes++;
            if ((nodes & 31) == 0 && OutOfTime()) return 0;
            if (kings[side] < 0) return -Mate + ply;
            if (!nullBranch && IsDraw(ply)) return 0;
            if (Attacked(kings[side ^ 1], side)) return Mate - ply - 1;
            bool check = Attacked(kings[side], side ^ 1);
            // At the horizon include quiet checks. Replies must still search every
            // evasion, including castling out of (or through) an attack.
            bool tacticalOnly = !check && qdepth != 0;
            int count = Generate(ply, tacticalOnly), start = ply * Stride;
            if (count == 0 && (!tacticalOnly || Generate(ply, false) == 0)) return 0;
            int stand = Evaluate();
            if (ply >= MaxPly - 2) return check ? Math.Min(stand, -1000) : stand;
            if (!check) {
                if (stand >= beta) return stand;
                if (stand > alpha) alpha = stand;
                if (qdepth >= 16) return stand;
            }
            Order(ply, count, 0);
            int best = check ? -Mate + ply + 2 : stand;
            for (int i = 0; i < count; i++) {
                if ((i & 7) == 0 && OutOfTime()) return 0;
                int move = Pick(start, i, count), to = (move >> 7) & 127;
                int gain = values[cells[to] & 7];
                bool promotion = Promotes(move);
                bool losingCapture = !check && gain != 0 && !promotion && gain < values[cells[move & 127] & 7] && Exchange(move) < 0;
                Make(move, ply);
                bool draw = !nullBranch && IsDraw(ply + 1);
                if (!draw && Attacked(kings[side ^ 1], side)) { Undo(move, ply); continue; }
                bool givesCheck = Attacked(kings[side], side ^ 1);
                if (!check && !promotion && !draw && !givesCheck && (gain == 0 || losingCapture || stand + gain + 180 < alpha)) { Undo(move, ply); continue; }
                int score = draw ? 0 : -Quiescence(ply + 1, -beta, -alpha, qdepth + 1, nullBranch);
                Undo(move, ply);
                if (stopped) return 0;
                if (score > best) best = score;
                if (score > alpha) alpha = score;
                if (alpha >= beta) break;
            }
            return best;
        }

        private bool Promotes(int move) => (cells[move & 127] & 7) == 1 && (((move >> 11) & 7) == (side == 0 ? 7 : 0));

        private void Order(int ply, int count, int preferred) {
            int start = ply * Stride;
            for (int i = 0; i < count; i++) {
                int move = moveBuffer[start + i], from = move & 127, to = (move >> 7) & 127;
                int victim = cells[to] & 7, type = cells[from] & 7;
                int score = history[(side * 128 + from) * 128 + to];
                if (victim != 0) {
                    score = 1000000 + values[victim] * 16 - values[type];
                    if (values[victim] < values[type] && Exchange(move) < 0) score -= 1050000;
                }
                if (Promotes(move)) score += 1500000;
                if (move == killers[ply * 2]) score += 80000;
                else if (move == killers[ply * 2 + 1]) score += 70000;
                if (move == preferred) score = 10000000;
                orderBuffer[start + i] = score;
            }
        }

        private int Pick(int start, int index, int count) {
            int best = start + index;
            for (int i = best + 1; i < start + count; i++) if (orderBuffer[i] > orderBuffer[best]) best = i;
            int target = start + index, move = moveBuffer[best];
            moveBuffer[best] = moveBuffer[target];
            moveBuffer[target] = move;
            orderBuffer[best] = orderBuffer[target];
            return move;
        }

        // Local swap-off analysis, used only for ordering and conservative
        // quiescence pruning. Checking sacrifices are never pruned by this test.
        private int Exchange(int move) {
            int from = move & 127, to = (move >> 7) & 127;
            int original = cells[from], captured = cells[to];
            Span<int> gains = stackalloc int[32];
            Span<int> removedSquares = stackalloc int[32];
            Span<int> removedPieces = stackalloc int[32];
            int depth = 0, color = side ^ 1;
            int placed = Promotes(move) ? 5 | (side << 3) : original;
            gains[0] = values[captured & 7] + values[placed & 7] - values[original & 7];
            cells[from] = 0;
            cells[to] = placed;
            while (depth < 30) {
                int attacker = LeastAttacker(to, color);
                if (attacker < 0) break;
                int piece = cells[attacker], promotion = (piece & 7) == 1 && (to >> 4) == (color == 0 ? 7 : 0) ? 890 : 0;
                cells[attacker] = 0;
                cells[to] = promotion != 0 ? 5 | (color << 3) : piece;
                if ((piece & 7) == 6 && Attacked(to, color ^ 1)) {
                    cells[attacker] = piece;
                    cells[to] = placed;
                    break;
                }
                removedSquares[depth] = attacker;
                removedPieces[depth] = piece;
                depth++;
                gains[depth] = values[placed & 7] + promotion - gains[depth - 1];
                placed = cells[to];
                color ^= 1;
            }
            for (int i = depth - 1; i >= 0; i--) {
                gains[i] = -Math.Max(-gains[i], gains[i + 1]);
                cells[removedSquares[i]] = removedPieces[i];
            }
            cells[from] = original;
            cells[to] = captured;
            return gains[0];
        }

        private int LeastAttacker(int square, int color) {
            int back = color == 0 ? -16 : 16;
            for (int dx = -1; dx <= 1; dx += 2) {
                int from = square + back + dx;
                if ((from & 0x88) == 0 && cells[from] == (1 | (color << 3))) return from;
            }
            for (int d = 0; d < 8; d++) {
                int from = square + knightSteps[d];
                if ((from & 0x88) == 0 && cells[from] == (2 | (color << 3))) return from;
            }
            int best = -1, price = int.MaxValue;
            for (int d = 0; d < 8; d++) {
                int step = raySteps[d], from = square + step, distance = 1;
                while ((from & 0x88) == 0) {
                    int piece = cells[from], type = piece & 7;
                    if (piece != 0) {
                        if (((piece >> 3) & 1) == color && (type == 5 || type == (d < 4 ? 4 : 3) || (type == 6 && distance == 1)) && values[type] < price) {
                            best = from;
                            price = values[type];
                        }
                        break;
                    }
                    from += step;
                    distance++;
                }
            }
            return best;
        }

        private bool PawnAttacked(int square, int color) {
            int step = color == 0 ? -16 : 16, pawn = 1 | (color << 3);
            int a = square + step - 1, b = square + step + 1;
            return ((a & 0x88) == 0 && cells[a] == pawn) || ((b & 0x88) == 0 && cells[b] == pawn);
        }

        private int Evaluate() {
            int m = mg, e = eg;
            Span<int> files = stackalloc int[16];
            files.Clear();
            for (int s = 0; s < 128; s++) {
                if ((s & 0x88) != 0) { s += 7; continue; }
                if ((cells[s] & 7) == 1) files[((cells[s] >> 3) & 1) * 8 + (s & 7)]++;
            }
            for (int color = 0; color < 2; color++) {
                int sign = color == 0 ? 1 : -1, offset = color * 8;
                if (counts[offset + 3] >= 2) { m += sign * 28; e += sign * 45; }
                for (int file = 0; file < 8; file++) {
                    if (files[offset + file] > 1) { m -= sign * 14 * (files[offset + file] - 1); e -= sign * 22 * (files[offset + file] - 1); }
                    if (files[offset + file] > 0 && (file == 0 || files[offset + file - 1] == 0) && (file == 7 || files[offset + file + 1] == 0)) {
                        m -= sign * 12 * files[offset + file]; e -= sign * 16 * files[offset + file];
                    }
                }
                int king = kings[color];
                if (king >= 0) {
                    int step = color == 0 ? 16 : -16, shield = 0;
                    for (int dx = -1; dx <= 1; dx++) {
                        int square = king + step + dx;
                        if ((square & 0x88) == 0 && cells[square] == (1 | offset)) shield += 12;
                        else {
                            square += step;
                            if ((square & 0x88) == 0 && cells[square] == (1 | offset)) shield += 5;
                        }
                        int file = (king & 7) + dx;
                        if (file >= 0 && file < 8 && files[offset + file] == 0) shield -= files[(offset ^ 8) + file] == 0 ? 16 : 10;
                    }
                    m += sign * shield;
                }
            }
            for (int from = 0; from < 128; from++) {
                if ((from & 0x88) != 0) { from += 7; continue; }
                int p = cells[from], type = p & 7;
                if (p == 0 || type == 6) continue;
                int color = (p >> 3) & 1, enemy = color ^ 1, sign = color == 0 ? 1 : -1;
                int rank = color == 0 ? from >> 4 : 7 - (from >> 4), file = from & 7;
                if (type == 1) {
                    bool passed = true;
                    int step = color == 0 ? 16 : -16;
                    for (int next = from + step; (next & 0x88) == 0 && passed; next += step)
                        for (int dx = -1; dx <= 1; dx++)
                            if (((next + dx) & 0x88) == 0 && cells[next + dx] == (1 | (enemy << 3))) { passed = false; break; }
                    if (passed) {
                        m += sign * (rank * rank * 3 + 4);
                        e += sign * (rank * rank * 7 + 8);
                        int front = from + step;
                        if ((front & 0x88) == 0 && cells[front] == 0) e += sign * rank * 5;
                        if (kings[enemy] >= 0 && kings[color] >= 0) {
                            int promotion = file + (color == 0 ? 112 : 0);
                            e += sign * (Distance(kings[enemy], promotion) - Distance(kings[color], promotion)) * rank * 3;
                        }
                    }
                    if (PawnAttacked(from, color)) { m += sign * 8; e += sign * 12; }
                    continue;
                }
                if (type == 4 && files[color * 8 + file] == 0) {
                    m += sign * (files[enemy * 8 + file] == 0 ? 25 : 12);
                    e += sign * 10;
                }
                int mobility = 0, pressure = 0;
                int first = type == 3 ? 4 : 0, end = type == 4 ? 4 : 8;
                for (int d = first; d < end; d++) {
                    int step = type == 2 ? knightSteps[d] : raySteps[d];
                    for (int to = from + step; (to & 0x88) == 0; to += step) {
                        int target = cells[to];
                        if (target != 0 && ((target >> 3) & 1) == color) break;
                        if (!PawnAttacked(to, enemy)) mobility++;
                        if (kings[enemy] >= 0 && Distance(to, kings[enemy]) <= 1) pressure++;
                        if (target != 0 || type == 2) break;
                    }
                }
                int weight = type == 2 ? 4 : type == 3 ? 4 : type == 4 ? 2 : 1;
                m += sign * (mobility * weight + pressure * (counts[enemy * 8 + 5] + counts[color * 8 + 5] > 0 ? 9 : 3));
                e += sign * mobility * (type == 5 ? 2 : 4);
            }
            int blend = Math.Min(24, phase);
            int result = (m * blend + e * (24 - blend)) / 24;
            return (side == 0 ? result : -result) + 12;
        }

        private int Distance(int a, int b) => Math.Max(Math.Abs((a & 7) - (b & 7)), Math.Abs((a >> 4) - (b >> 4)));
    }
}
