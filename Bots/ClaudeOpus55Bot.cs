using DChess.BotApi;
using DChess.Chess.Pieces;
using DChess.Chess.Playground;
using DChess.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace DChess.Bots {
	/// <summary>
	/// Bitboard alpha-beta engine with a rich tapered evaluation.
	/// Game rules handled here: the king is captured (no check rules), promotion is always to a queen,
	/// no en passant, castling ignores attacked squares, and a side without a safe move must move into capture.
	/// The engine plays on its own board representation and maps its choice back to a move of the API.
	/// </summary>
	public class ClaudeOpus55Bot : IChessBot {
		public string Name => "ClaudeOpus55Bot";

		// =====================================================================================
		// Constants
		// =====================================================================================

		private const int PAWN = 1, KNIGHT = 2, BISHOP = 3, ROOK = 4, QUEEN = 5, KING = 6;
		private const int MAXPLY = 100;
		private const int MAXDEPTH = 64;
		private const int MOVES_PER_PLY = 256;
		private const int INF = 32000;
		private const int MATE = 31000;
		private const int MATE_BOUND = MATE - 2 * MAXPLY;
		private const int TT_SIZE = 1 << 20;
		private const int PH_SIZE = 1 << 14;
		private const int EC_SIZE = 1 << 16;
		private const int HIST_SIZE = 1024;
		private const int STACK = MAXPLY + 16;
		private const int BOUND_UPPER = 1, BOUND_LOWER = 2, BOUND_EXACT = 3;
		private const int ROOK_TABLE = 102400, BISHOP_TABLE = 5248;
		private const int SCORE_TT = 3000000, SCORE_GOOD_CAPTURE = 2000000, SCORE_KILLER1 = 1900000;
		private const int SCORE_KILLER2 = 1800000, SCORE_COUNTER = 1700000, SCORE_BAD_CAPTURE = -1000000;

		private const ulong FILE_A = 0x0101010101010101UL;
		private const ulong FILE_H = 0x8080808080808080UL;
		private const ulong RANK_1 = 0x00000000000000FFUL;
		private const ulong RANK_2 = 0x000000000000FF00UL;
		private const ulong RANK_3 = 0x0000000000FF0000UL;
		private const ulong RANK_6 = 0x0000FF0000000000UL;
		private const ulong RANK_7 = 0x00FF000000000000UL;
		private const ulong RANK_8 = 0xFF00000000000000UL;
		private const ulong LIGHT_SQUARES = 0x55AA55AA55AA55AAUL;
		private const ulong CENTER4 = 0x0000001818000000UL;
		private const ulong WHITE_OUTPOST_RANKS = 0x0000FFFFFF000000UL;
		private const ulong BLACK_OUTPOST_RANKS = 0x000000FFFFFF0000UL;
		private const ulong WHITE_SPACE = 0x000000003C3C3C00UL;
		private const ulong BLACK_SPACE = 0x003C3C3C00000000UL;

		// Evaluation scalars (centipawn-like units).
		private const int TEMPO = 16;
		private const int KW_KNIGHT = 62, KW_BISHOP = 46, KW_ROOK = 44, KW_QUEEN = 24;

		// =====================================================================================
		// Tables (instance fields: the rules forbid static fields)
		// =====================================================================================

		private readonly ulong[] knightAtt = new ulong[64];
		private readonly ulong[] kingAtt = new ulong[64];
		private readonly ulong[] pawnAtt = new ulong[128];     // [color * 64 + square]: squares a pawn attacks
		private readonly ulong[] rMask = new ulong[64];
		private readonly ulong[] bMask = new ulong[64];
		private readonly int[] rShift = new int[64];
		private readonly int[] bShift = new int[64];
		private readonly int[] rOff = new int[64];
		private readonly int[] bOff = new int[64];
		private readonly ulong[] slide = new ulong[ROOK_TABLE + BISHOP_TABLE];
		private readonly ulong[] zob = new ulong[16 * 64];
		private readonly ulong[] zobCastle = new ulong[64];
		private ulong zobSide;
		private readonly int[] castleMask = new int[64];
		private readonly int[] pstMg = new int[16 * 64];
		private readonly int[] pstEg = new int[16 * 64];
		private readonly int[] phaseInc = new int[16];
		private readonly int[] seeVal = { 0, 100, 320, 330, 500, 950, 20000, 0 };
		private readonly ulong[] passedMask = new ulong[128];  // squares in front on own and adjacent files
		private readonly ulong[] frontFile = new ulong[128];   // squares in front on the same file
		private readonly ulong[] forwardRanks = new ulong[128];
		private readonly ulong[] fileMask = new ulong[8];
		private readonly ulong[] adjFiles = new ulong[8];
		private readonly int[] dist = new int[64 * 64];
		private readonly int[] centerManhattan = new int[64];
		private readonly int[] lmr = new int[64 * 64];

		// Magic multipliers found offline with a small random search written for this bot.
		private readonly ulong[] rMagic = {
			0xB08002108029C002UL, 0x0540002008100040UL, 0x0200104200200881UL, 0x01803000800C0800UL,
			0x1280040080220801UL, 0x060010040A000821UL, 0x0400141012008821UL, 0xE20001C2008CE401UL,
			0x8461800040002286UL, 0x8100402000401000UL, 0x0000802000100081UL, 0x0001001000200900UL,
			0x0080800400800800UL, 0x0000800200800400UL, 0x000D000200041100UL, 0x0802001104204082UL,
			0x0000888000400020UL, 0x0010004040002000UL, 0x1810120040220082UL, 0x4806020020100840UL,
			0x8011010004080210UL, 0x0004008080040200UL, 0x2008040028020130UL, 0x1100120010410084UL,
			0x2001401180008020UL, 0x0400200040401000UL, 0x0100104100200100UL, 0x0410001100200900UL,
			0x0208020040040040UL, 0x0000020080800400UL, 0x0110010400020810UL, 0x2080004200010084UL,
			0x0280002000400040UL, 0x0000402008401000UL, 0x0014842000801000UL, 0x0080100101000820UL,
			0x0040800402800801UL, 0x8804000480800200UL, 0x0000011004004208UL, 0x02001441020004A4UL,
			0x0040084080248005UL, 0x8000410082020020UL, 0x0010080024002002UL, 0x001010002101000AUL,
			0x0002012010860008UL, 0x090A000400028080UL, 0x8060100608540023UL, 0x0000086102860004UL,
			0x0080008100402100UL, 0x4102400080200580UL, 0x0001200502401300UL, 0x2080100180080480UL,
			0x4C00800400080080UL, 0x0800020080040080UL, 0x6008900802310400UL, 0x180091008404CA00UL,
			0x0800481021008001UL, 0x080200408A201102UL, 0x0004810840120222UL, 0x100D022008041001UL,
			0x0006003420481006UL, 0x010B000C00066809UL, 0x00010004020000A1UL, 0x08000D0420840042UL,
		};
		private readonly ulong[] bMagic = {
			0x0248200406A02100UL, 0x9044240800410180UL, 0x0084080881040008UL, 0x0820A10044002003UL,
			0x0004042000002110UL, 0x0040825040000003UL, 0x000C049209200890UL, 0x0002038C03251000UL,
			0x1202100288010401UL, 0x0A92020801040088UL, 0x1208221803022000UL, 0x028408048B000221UL,
			0x4203040422085200UL, 0x3000008820888021UL, 0x0000010908124000UL, 0x0000B4804402A001UL,
			0x0850204110C90100UL, 0x01100060040090B0UL, 0x00880010104C4088UL, 0x980C00080C121284UL,
			0x0901000820084104UL, 0x0008804410041102UL, 0x0804700401041041UL, 0x001D100482411040UL,
			0x4020040008880800UL, 0x000820018202020CUL, 0x000C010002080100UL, 0x0004004004010002UL,
			0x0190040000802100UL, 0x2010010000804102UL, 0x0184044109280204UL, 0x8024085000210410UL,
			0x0001244000600880UL, 0x0200888402A00400UL, 0x1004082204340400UL, 0x4400020084080080UL,
			0x0110010040100404UL, 0x4008260120041002UL, 0xC40228D400070400UL, 0x0041040184210264UL,
			0x0108015090020821UL, 0x0600922150102001UL, 0x020A01620800090AUL, 0x0014C0201108580CUL,
			0x4083040102100C00UL, 0x1028101082020020UL, 0x8420010420800900UL, 0x04104228810A0020UL,
			0x0002009004100008UL, 0xA000261802180000UL, 0x0004220062080008UL, 0x0110201420880900UL,
			0x108004104202080AUL, 0x4000100210010440UL, 0x8004081024008001UL, 0x0020020419002080UL,
			0x0041840088142281UL, 0x0001084200842002UL, 0x0000240025084810UL, 0x8010800100208800UL,
			0x0080000004050403UL, 0x0001000404082200UL, 0x0010C00288422080UL, 0x8004014802068202UL,
		};

		// Mobility bonuses by number of reachable squares (own values).
		private readonly int[] nMobMg = { -46, -26, -11, -2, 6, 13, 19, 24, 28 };
		private readonly int[] nMobEg = { -58, -32, -16, -5, 4, 11, 16, 20, 22 };
		private readonly int[] bMobMg = { -42, -24, -9, 1, 9, 16, 22, 27, 31, 34, 37, 39, 41, 43 };
		private readonly int[] bMobEg = { -56, -33, -16, -4, 5, 13, 20, 25, 30, 34, 37, 39, 41, 42 };
		private readonly int[] rMobMg = { -32, -19, -9, -4, -1, 2, 6, 10, 14, 17, 20, 23, 25, 27, 28 };
		private readonly int[] rMobEg = { -62, -36, -16, -1, 11, 21, 30, 38, 45, 51, 56, 60, 63, 65, 66 };
		private readonly int[] qMobMg = { -26, -16, -9, -5, -1, 2, 5, 8, 11, 13, 15, 17, 19, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 33, 34 };
		private readonly int[] qMobEg = { -46, -31, -19, -9, -1, 7, 14, 21, 27, 32, 37, 41, 45, 49, 52, 55, 57, 59, 61, 63, 65, 66, 67, 68, 69, 70, 71, 71 };

		// Pawn structure (index = relative rank 0..7).
		private readonly int[] passedMg = { 0, 3, 6, 10, 22, 44, 72, 0 };
		private readonly int[] passedEg = { 0, 8, 12, 20, 38, 68, 112, 0 };
		private readonly int[] connectedBonus = { 0, 4, 6, 10, 18, 31, 50, 0 };
		private readonly int[] passerWeight = { 0, 0, 0, 1, 3, 6, 10, 0 };
		// King shelter: own pawn relative rank on a file near the king (0 = none), enemy storm pawn rank.
		private readonly int[] shelterTab = { -36, 30, 21, 6, -6, -14, -18, -18 };
		private readonly int[] stormOpen = { 0, 0, -38, -28, -15, -6, 0, 0 };
		private readonly int[] stormBlocked = { 0, 0, 0, -12, -6, 0, 0, 0 };
		// Threats by victim type (index = piece type).
		private readonly int[] thrMinorMg = { 0, 4, 26, 28, 52, 48, 0, 0 };
		private readonly int[] thrMinorEg = { 0, 20, 32, 36, 72, 90, 0, 0 };
		private readonly int[] thrRookMg = { 0, 2, 22, 22, 0, 40, 0, 0 };
		private readonly int[] thrRookEg = { 0, 24, 36, 36, 18, 40, 0, 0 };

		// =====================================================================================
		// Board state
		// =====================================================================================

		private readonly ulong[] bb = new ulong[16];   // [color * 8 + type]; index color * 8 holds all pieces of the color
		private readonly int[] sq = new int[64];       // piece code (color * 8 + type) or 0
		private int side, castle, halfmove;
		private ulong hash, pawnKey;
		private int psqMg, psqEg, phase;
		private int sp;
		private readonly int[] uCastle = new int[STACK];
		private readonly int[] uHalf = new int[STACK];
		private readonly int[] uMg = new int[STACK];
		private readonly int[] uEg = new int[STACK];
		private readonly int[] uPhase = new int[STACK];
		private readonly ulong[] uHash = new ulong[STACK];
		private readonly ulong[] uPawn = new ulong[STACK];

		// =====================================================================================
		// Search state
		// =====================================================================================

		private struct TTEntry {
			public ulong Key;
			public int Move;
			public short Score;
			public short Eval;
			public byte Depth;
			public byte Bound;
			public byte Age;
		}

		private readonly TTEntry[] tt = new TTEntry[TT_SIZE];
		private byte ttAge;
		private readonly ulong[] phKey = new ulong[PH_SIZE];
		private readonly int[] phMg = new int[PH_SIZE];
		private readonly int[] phEg = new int[PH_SIZE];
		private readonly ulong[] phPassed = new ulong[PH_SIZE];
		private readonly ulong[] phSpan = new ulong[PH_SIZE * 2];
		private readonly ulong[] ecKey = new ulong[EC_SIZE];
		private readonly int[] ecVal = new int[EC_SIZE];
		private readonly int[] moveBuf = new int[MAXPLY * MOVES_PER_PLY];
		private readonly int[] scoreBuf = new int[MAXPLY * MOVES_PER_PLY];
		private readonly int[] quietBuf = new int[MAXPLY * 64];
		private readonly int[] killer1 = new int[MAXPLY + 1];
		private readonly int[] killer2 = new int[MAXPLY + 1];
		private readonly int[] history = new int[16 * 64];
		private readonly int[] counterMove = new int[16 * 64];
		private readonly int[] contHist = new int[1024 * 1024];   // [previous piece-to][piece-to], one and two plies back
		private readonly int[] playedMove = new int[MAXPLY + 1];
		private readonly int[] evalStack = new int[MAXPLY + 1];
		private readonly ulong[] repHash = new ulong[HIST_SIZE + MAXPLY + 8];  // game history followed by the search path
		private int rootIdx, rootGamePly;
		private long nodes, nodeLimit, deadline;
		private bool stop;
		private readonly int[] rootMoves = new int[MOVES_PER_PLY];
		private readonly long[] rootNodes = new long[MOVES_PER_PLY];
		private int rootCount, rootBest;
		private bool searchFailed;

		// Evaluation scratch.
		private readonly ulong[] attBy = new ulong[16];
		private readonly ulong[] att2 = new ulong[2];
		private readonly int[] kAttCnt = new int[2];
		private readonly int[] kAttW = new int[2];
		private readonly int[] kAttHits = new int[2];

		// Game history between Think calls.
		private int gameHistCount;
		private bool histValid;
		private int lastPly = -10;
		private ulong lastRootHash, lastAfterHash, lastAfterPawns, startHash;
		private int lastAfterHalf, lastAfterPieceCount;
		private readonly int[] verifyA = new int[MOVES_PER_PLY];
		private readonly int[] verifyB = new int[MOVES_PER_PLY];
		private readonly bool engineOk;

		// =====================================================================================
		// Construction
		// =====================================================================================

		public ClaudeOpus55Bot() {
			try {
				InitTables();
				InitEval();
				SetStartPosition();
				startHash = hash;
				engineOk = true;
				WarmUp();
			}
			catch (Exception) {
				engineOk = false;
			}
		}

		private void InitTables() {
			for (int s = 0; s < 64; s++) {
				int x = s & 7, y = s >> 3;
				ulong n = 0, k = 0;
				for (int dx = -2; dx <= 2; dx++) {
					for (int dy = -2; dy <= 2; dy++) {
						int tx = x + dx, ty = y + dy;
						if (tx < 0 || tx > 7 || ty < 0 || ty > 7) continue;
						int ax = Math.Abs(dx), ay = Math.Abs(dy);
						if (ax + ay == 3) n |= 1UL << (ty * 8 + tx);
						if (ax <= 1 && ay <= 1 && ax + ay > 0) k |= 1UL << (ty * 8 + tx);
					}
				}
				knightAtt[s] = n;
				kingAtt[s] = k;
				ulong w = 0, b = 0;
				if (y < 7) {
					if (x > 0) w |= 1UL << (s + 7);
					if (x < 7) w |= 1UL << (s + 9);
				}
				if (y > 0) {
					if (x > 0) b |= 1UL << (s - 9);
					if (x < 7) b |= 1UL << (s - 7);
				}
				pawnAtt[s] = w;
				pawnAtt[64 + s] = b;
				for (int t = 0; t < 64; t++) {
					int tx = t & 7, ty = t >> 3;
					dist[s * 64 + t] = Math.Max(Math.Abs(tx - x), Math.Abs(ty - y));
				}
				centerManhattan[s] = Math.Max(3 - x, x - 4) + Math.Max(3 - y, y - 4);
			}
			for (int f = 0; f < 8; f++) {
				fileMask[f] = FILE_A << f;
			}
			for (int f = 0; f < 8; f++) {
				adjFiles[f] = (f > 0 ? fileMask[f - 1] : 0) | (f < 7 ? fileMask[f + 1] : 0);
			}
			for (int s = 0; s < 64; s++) {
				int f = s & 7, r = s >> 3;
				ulong wf = 0, bf = 0;
				for (int rr = r + 1; rr < 8; rr++) wf |= 0xFFUL << (rr * 8);
				for (int rr = r - 1; rr >= 0; rr--) bf |= 0xFFUL << (rr * 8);
				forwardRanks[s] = wf;
				forwardRanks[64 + s] = bf;
				frontFile[s] = wf & fileMask[f];
				frontFile[64 + s] = bf & fileMask[f];
				passedMask[s] = wf & (fileMask[f] | adjFiles[f]);
				passedMask[64 + s] = bf & (fileMask[f] | adjFiles[f]);
			}

			// Magic bitboards.
			int off = 0;
			for (int s = 0; s < 64; s++) {
				rMask[s] = SlideMask(s, true);
				int bits = BitOperations.PopCount(rMask[s]);
				rShift[s] = 64 - bits;
				rOff[s] = off;
				off += 1 << bits;
			}
			if (off != ROOK_TABLE) throw new InvalidOperationException("rook table");
			for (int s = 0; s < 64; s++) {
				bMask[s] = SlideMask(s, false);
				int bits = BitOperations.PopCount(bMask[s]);
				bShift[s] = 64 - bits;
				bOff[s] = off;
				off += 1 << bits;
			}
			if (off != ROOK_TABLE + BISHOP_TABLE) throw new InvalidOperationException("bishop table");
			for (int s = 0; s < 64; s++) {
				FillSlide(s, true, rMask[s], rMagic[s], rShift[s], rOff[s]);
				FillSlide(s, false, bMask[s], bMagic[s], bShift[s], bOff[s]);
			}

			// Zobrist keys from a fixed xorshift sequence.
			ulong seed = 0x9E3779B97F4A7C15UL;
			for (int i = 0; i < zob.Length; i++) zob[i] = NextRandom(ref seed);
			for (int i = 1; i < 64; i++) zobCastle[i] = NextRandom(ref seed);
			zobSide = NextRandom(ref seed);

			for (int s = 0; s < 64; s++) castleMask[s] = 63;
			castleMask[4] = 63 & ~1;
			castleMask[0] = 63 & ~2;
			castleMask[7] = 63 & ~4;
			castleMask[60] = 63 & ~8;
			castleMask[56] = 63 & ~16;
			castleMask[63] = 63 & ~32;

			for (int d = 1; d < 64; d++) {
				for (int m = 1; m < 64; m++) {
					lmr[d * 64 + m] = (int)(0.75 + Math.Log(d) * Math.Log(m) / 2.25);
				}
			}
		}

		private static ulong NextRandom(ref ulong state) {
			state ^= state >> 12;
			state ^= state << 25;
			state ^= state >> 27;
			return state * 0x2545F4914F6CDD1DUL;
		}

		private static void Direction(int d, bool rook, out int dx, out int dy) {
			if (rook) {
				dx = d == 0 ? 1 : d == 1 ? -1 : 0;
				dy = d == 2 ? 1 : d == 3 ? -1 : 0;
			}
			else {
				dx = (d & 1) == 0 ? 1 : -1;
				dy = (d & 2) == 0 ? 1 : -1;
			}
		}

		private static ulong SlideMask(int s, bool rook) {
			ulong m = 0;
			int x0 = s & 7, y0 = s >> 3;
			for (int d = 0; d < 4; d++) {
				Direction(d, rook, out int dx, out int dy);
				int x = x0 + dx, y = y0 + dy;
				// Stop before the edge: the last square of a ray never changes the attack set.
				while (x >= 0 && x <= 7 && y >= 0 && y <= 7 && x + dx >= 0 && x + dx <= 7 && y + dy >= 0 && y + dy <= 7) {
					m |= 1UL << (y * 8 + x);
					x += dx;
					y += dy;
				}
			}
			return m;
		}

		private static ulong SlideAttackSlow(int s, ulong occ, bool rook) {
			ulong a = 0;
			int x0 = s & 7, y0 = s >> 3;
			for (int d = 0; d < 4; d++) {
				Direction(d, rook, out int dx, out int dy);
				int x = x0 + dx, y = y0 + dy;
				while (x >= 0 && x <= 7 && y >= 0 && y <= 7) {
					ulong bit = 1UL << (y * 8 + x);
					a |= bit;
					if ((occ & bit) != 0) break;
					x += dx;
					y += dy;
				}
			}
			return a;
		}

		private void FillSlide(int s, bool rook, ulong mask, ulong magic, int shift, int offset) {
			ulong sub = 0;
			do {
				ulong att = SlideAttackSlow(s, sub, rook);
				int idx = offset + (int)((sub * magic) >> shift);
				if (slide[idx] == 0) slide[idx] = att;
				else if (slide[idx] != att) throw new InvalidOperationException("bad magic");
				sub = (sub - mask) & mask;
			} while (sub != 0);
		}

		private void InitEval() {
			// Piece-square tables written from White's point of view, rank 8 first (as seen on a diagram).
			int[] pawnMg = {
				  0,   0,   0,   0,   0,   0,   0,   0,
				 50,  60,  55,  65,  65,  55,  60,  50,
				 12,  18,  26,  36,  36,  26,  18,  12,
				  0,   4,  10,  22,  22,  10,   4,   0,
				 -6,  -2,   6,  18,  18,   4,  -2,  -6,
				 -6,  -4,   2,   4,   4,  -2,  -2,  -6,
				 -8,  -2,  -4, -14, -14,   8,   6,  -6,
				  0,   0,   0,   0,   0,   0,   0,   0 };
			int[] pawnEg = {
				  0,   0,   0,   0,   0,   0,   0,   0,
				 70,  70,  65,  60,  60,  65,  70,  70,
				 38,  36,  30,  26,  26,  30,  36,  38,
				 16,  14,  10,   6,   6,  10,  14,  16,
				  4,   4,   0,  -2,  -2,   0,   4,   4,
				 -4,  -2,  -4,  -2,  -2,  -4,  -2,  -4,
				 -4,  -2,   0,   2,   2,   0,  -2,  -4,
				  0,   0,   0,   0,   0,   0,   0,   0 };
			int[] knightMg = {
				-70, -35, -25, -18, -18, -25, -35, -70,
				-30, -15,   2,   8,   8,   2, -15, -30,
				-15,   6,  16,  24,  24,  16,   6, -15,
				 -8,  10,  20,  28,  28,  20,  10,  -8,
				-12,   6,  16,  22,  22,  16,   6, -12,
				-22,   0,  12,  14,  14,  12,   0, -22,
				-30, -16,  -4,   4,   4,  -4, -16, -30,
				-60, -26, -22, -16, -16, -22, -26, -60 };
			int[] knightEg = {
				-50, -32, -22, -16, -16, -22, -32, -50,
				-28, -12,  -2,   4,   4,  -2, -12, -28,
				-18,   0,  10,  16,  16,  10,   0, -18,
				-14,   4,  14,  20,  20,  14,   4, -14,
				-14,   4,  14,  20,  20,  14,   4, -14,
				-18,  -2,   6,  12,  12,   6,  -2, -18,
				-28, -12,  -4,   0,   0,  -4, -12, -28,
				-50, -34, -24, -18, -18, -24, -34, -50 };
			int[] bishopMg = {
				-22, -10, -12, -10, -10, -12, -10, -22,
				-12,  -2,   0,   2,   2,   0,  -2, -12,
				 -8,   6,  10,  10,  10,  10,   6,  -8,
				 -6,   8,  10,  16,  16,  10,   8,  -6,
				 -6,   6,  12,  16,  16,  12,   6,  -6,
				 -4,  10,   8,  10,  10,   8,  10,  -4,
				 -6,  14,   6,   4,   4,   6,  14,  -6,
				-20,  -8, -12, -10, -10, -12,  -8, -20 };
			int[] bishopEg = {
				-18, -10,  -8,  -6,  -6,  -8, -10, -18,
				-10,  -4,   0,   2,   2,   0,  -4, -10,
				 -6,   2,   6,   6,   6,   6,   2,  -6,
				 -4,   2,   6,  10,  10,   6,   2,  -4,
				 -4,   2,   6,  10,  10,   6,   2,  -4,
				 -6,   2,   6,   6,   6,   6,   2,  -6,
				-10,  -4,   0,   2,   2,   0,  -4, -10,
				-18, -10,  -8,  -6,  -6,  -8, -10, -18 };
			int[] rookMg = {
				  6,   6,   8,  12,  12,   8,   6,   6,
				 16,  20,  22,  24,  24,  22,  20,  16,
				 -2,   4,   6,   8,   8,   6,   4,  -2,
				 -8,  -2,   0,   2,   2,   0,  -2,  -8,
				-12,  -6,  -2,   0,   0,  -2,  -6, -12,
				-14,  -8,  -4,  -2,  -2,  -4,  -8, -14,
				-16, -10,  -4,  -2,  -2,  -4, -10, -16,
				 -6,  -4,   2,   8,   8,   4,  -4,  -6 };
			int[] rookEg = {
				  8,   8,   8,   8,   8,   8,   8,   8,
				 14,  14,  14,  14,  14,  14,  14,  14,
				  4,   4,   4,   4,   4,   4,   4,   4,
				  2,   2,   2,   2,   2,   2,   2,   2,
				  0,   0,   0,   0,   0,   0,   0,   0,
				 -2,  -2,  -2,  -2,  -2,  -2,  -2,  -2,
				 -4,  -4,  -4,  -4,  -4,  -4,  -4,  -4,
				 -6,  -4,  -2,   0,   0,  -2,  -4,  -6 };
			int[] queenMg = {
				-12,  -6,  -4,  -2,  -2,  -4,  -6, -12,
				-10, -14,   0,   2,   2,   0, -14, -10,
				 -6,   0,   4,   6,   6,   4,   0,  -6,
				 -4,   0,   4,   6,   6,   4,   0,  -4,
				 -4,   0,   4,   6,   6,   4,   0,  -4,
				 -6,   2,   4,   4,   4,   4,   2,  -6,
				-10,  -2,   4,   2,   2,  -2,  -2, -10,
				-16, -12,  -6,   0,  -6, -12, -12, -16 };
			int[] queenEg = {
				-22, -12,  -8,  -4,  -4,  -8, -12, -22,
				-12,  -2,   4,   8,   8,   4,  -2, -12,
				 -8,   4,  10,  14,  14,  10,   4,  -8,
				 -4,   8,  14,  18,  18,  14,   8,  -4,
				 -4,   8,  14,  18,  18,  14,   8,  -4,
				 -8,   4,  10,  14,  14,  10,   4,  -8,
				-12,  -4,   2,   4,   4,   2,  -4, -12,
				-24, -14, -10,  -6,  -6, -10, -14, -24 };
			int[] kingMg = {
				-65, -70, -72, -80, -80, -72, -70, -65,
				-55, -62, -66, -72, -72, -66, -62, -55,
				-48, -54, -58, -66, -66, -58, -54, -48,
				-42, -46, -52, -60, -60, -52, -46, -42,
				-36, -40, -46, -54, -54, -46, -40, -36,
				-22, -26, -32, -40, -40, -32, -26, -22,
				 -2,   0, -12, -28, -28, -14,   2,   0,
				 14,  30,  12, -12,   0,   6,  34,  18 };
			int[] kingEg = {
				-55, -36, -26, -20, -20, -26, -36, -55,
				-30, -10,   0,   6,   6,   0, -10, -30,
				-20,   6,  16,  22,  22,  16,   6, -20,
				-16,  10,  24,  30,  30,  24,  10, -16,
				-18,   6,  20,  28,  28,  20,   6, -18,
				-24,  -4,  10,  16,  16,  10,  -4, -24,
				-34, -16,  -4,   2,   2,  -4, -16, -34,
				-56, -36, -26, -20, -20, -26, -36, -56 };
			int[] matMg = { 0, 85, 330, 350, 470, 1000, 0 };
			int[] matEg = { 0, 115, 310, 335, 540, 1010, 0 };
			int[][] tMg = { null, pawnMg, knightMg, bishopMg, rookMg, queenMg, kingMg };
			int[][] tEg = { null, pawnEg, knightEg, bishopEg, rookEg, queenEg, kingEg };
			for (int t = 1; t <= 6; t++) {
				for (int s = 0; s < 64; s++) {
					int x = s & 7, y = s >> 3;
					int wi = (7 - y) * 8 + x;   // table index for a white piece on s
					int bi = y * 8 + x;         // mirrored index for a black piece on s
					pstMg[t * 64 + s] = matMg[t] + tMg[t][wi];
					pstEg[t * 64 + s] = matEg[t] + tEg[t][wi];
					pstMg[(8 + t) * 64 + s] = -(matMg[t] + tMg[t][bi]);
					pstEg[(8 + t) * 64 + s] = -(matEg[t] + tEg[t][bi]);
				}
			}
			phaseInc[KNIGHT] = phaseInc[8 + KNIGHT] = 1;
			phaseInc[BISHOP] = phaseInc[8 + BISHOP] = 1;
			phaseInc[ROOK] = phaseInc[8 + ROOK] = 2;
			phaseInc[QUEEN] = phaseInc[8 + QUEEN] = 4;
		}

		private void SetStartPosition() {
			Array.Clear(bb, 0, bb.Length);
			Array.Clear(sq, 0, sq.Length);
			int[] back = { ROOK, KNIGHT, BISHOP, QUEEN, KING, BISHOP, KNIGHT, ROOK };
			for (int x = 0; x < 8; x++) {
				Put(x, back[x]);
				Put(8 + x, PAWN);
				Put(48 + x, 8 | PAWN);
				Put(56 + x, 8 | back[x]);
			}
			castle = 63;
			side = 0;
			halfmove = 0;
			sp = 0;
			ComputeKeys();
		}

		private void Put(int s, int pc) {
			sq[s] = pc;
			bb[pc] |= 1UL << s;
			bb[pc & 8] |= 1UL << s;
		}

		private void ComputeKeys() {
			hash = 0;
			pawnKey = 0;
			psqMg = psqEg = phase = 0;
			for (int s = 0; s < 64; s++) {
				int pc = sq[s];
				if (pc == 0) continue;
				hash ^= zob[pc * 64 + s];
				if ((pc & 7) == PAWN) pawnKey ^= zob[pc * 64 + s];
				psqMg += pstMg[pc * 64 + s];
				psqEg += pstEg[pc * 64 + s];
				phase += phaseInc[pc];
			}
			hash ^= zobCastle[castle];
			if (side == 1) hash ^= zobSide;
		}

		private void WarmUp() {
			// JIT-compiles the hot code before the first timed move (the constructor is not timed).
			long now = Stopwatch.GetTimestamp();
			rootIdx = 0;
			rootGamePly = 0;
			gameHistCount = 0;
			SearchPosition(now + Stopwatch.Frequency / 5, now + Stopwatch.Frequency / 10, 6000);
			SetStartPosition();
			Array.Clear(tt, 0, tt.Length);
			Array.Clear(history, 0, history.Length);
			Array.Clear(counterMove, 0, counterMove.Length);
			Array.Clear(contHist, 0, contHist.Length);
			ttAge = 0;
		}

		// =====================================================================================
		// Moves: from | to << 6 | piece << 12 | captured << 16 | flag << 20 (1 = promotion, 2 = castling)
		// =====================================================================================

		private void MakeMove(int m) {
			int from = m & 63, to = (m >> 6) & 63, pc = (m >> 12) & 15, cap = (m >> 16) & 15, flag = m >> 20;
			int s = sp++;
			uCastle[s] = castle;
			uHash[s] = hash;
			uPawn[s] = pawnKey;
			uMg[s] = psqMg;
			uEg[s] = psqEg;
			uPhase[s] = phase;
			uHalf[s] = halfmove;
			int us = pc & 8;
			ulong toBB = 1UL << to;
			ulong fromTo = (1UL << from) | toBB;
			halfmove++;
			if (cap != 0) {
				int ci = cap * 64 + to;
				bb[cap] ^= toBB;
				bb[cap & 8] ^= toBB;
				hash ^= zob[ci];
				psqMg -= pstMg[ci];
				psqEg -= pstEg[ci];
				phase -= phaseInc[cap];
				if ((cap & 7) == PAWN) pawnKey ^= zob[ci];
				halfmove = 0;
			}
			int fi = pc * 64 + from, ti = pc * 64 + to;
			bb[pc] ^= fromTo;
			bb[us] ^= fromTo;
			sq[from] = 0;
			sq[to] = pc;
			hash ^= zob[fi] ^ zob[ti];
			psqMg += pstMg[ti] - pstMg[fi];
			psqEg += pstEg[ti] - pstEg[fi];
			if ((pc & 7) == PAWN) {
				halfmove = 0;
				pawnKey ^= zob[fi] ^ zob[ti];
				if (flag == 1) {
					int q = us | QUEEN, qi = q * 64 + to;
					bb[pc] ^= toBB;
					bb[q] ^= toBB;
					sq[to] = q;
					hash ^= zob[ti] ^ zob[qi];
					pawnKey ^= zob[ti];
					psqMg += pstMg[qi] - pstMg[ti];
					psqEg += pstEg[qi] - pstEg[ti];
					phase += phaseInc[q];
				}
			}
			else if (flag == 2) {
				int rf, rt;
				if (to == 6) { rf = 7; rt = 5; }
				else if (to == 2) { rf = 0; rt = 3; }
				else if (to == 62) { rf = 63; rt = 61; }
				else { rf = 56; rt = 59; }
				int rp = us | ROOK, rfi = rp * 64 + rf, rti = rp * 64 + rt;
				ulong rft = (1UL << rf) | (1UL << rt);
				bb[rp] ^= rft;
				bb[us] ^= rft;
				sq[rf] = 0;
				sq[rt] = rp;
				hash ^= zob[rfi] ^ zob[rti];
				psqMg += pstMg[rti] - pstMg[rfi];
				psqEg += pstEg[rti] - pstEg[rfi];
			}
			if (castle != 0) {
				int nc = castle & castleMask[from] & castleMask[to];
				if (nc != castle) {
					hash ^= zobCastle[castle] ^ zobCastle[nc];
					castle = nc;
				}
			}
			side ^= 1;
			hash ^= zobSide;
		}

		private void UnmakeMove(int m) {
			int from = m & 63, to = (m >> 6) & 63, pc = (m >> 12) & 15, cap = (m >> 16) & 15, flag = m >> 20;
			int s = --sp;
			side ^= 1;
			int us = pc & 8;
			ulong toBB = 1UL << to;
			ulong fromTo = (1UL << from) | toBB;
			if (flag == 1) {
				int q = us | QUEEN;
				bb[q] ^= toBB;
				bb[pc] ^= toBB;
			}
			else if (flag == 2) {
				int rf, rt;
				if (to == 6) { rf = 7; rt = 5; }
				else if (to == 2) { rf = 0; rt = 3; }
				else if (to == 62) { rf = 63; rt = 61; }
				else { rf = 56; rt = 59; }
				int rp = us | ROOK;
				ulong rft = (1UL << rf) | (1UL << rt);
				bb[rp] ^= rft;
				bb[us] ^= rft;
				sq[rt] = 0;
				sq[rf] = rp;
			}
			bb[pc] ^= fromTo;
			bb[us] ^= fromTo;
			sq[from] = pc;
			sq[to] = cap;
			if (cap != 0) {
				bb[cap] ^= toBB;
				bb[cap & 8] ^= toBB;
			}
			castle = uCastle[s];
			hash = uHash[s];
			pawnKey = uPawn[s];
			psqMg = uMg[s];
			psqEg = uEg[s];
			phase = uPhase[s];
			halfmove = uHalf[s];
		}

		private void MakeNull() {
			int s = sp++;
			uHash[s] = hash;
			uHalf[s] = halfmove;
			side ^= 1;
			hash ^= zobSide;
			halfmove = 0;   // repetitions never reach back across a null move
		}

		private void UnmakeNull() {
			int s = --sp;
			side ^= 1;
			hash = uHash[s];
			halfmove = uHalf[s];
		}

		/// <summary>Generates pseudo-legal moves of the side to move into moveBuf[baseIdx..]; returns the count.
		/// Without quiets only captures and promotions are generated.</summary>
		private int GenMoves(int baseIdx, bool quiets) {
			int[] ml = moveBuf;
			int n = baseIdx;
			int us = side, u8 = us << 3, t8 = u8 ^ 8;
			ulong own = bb[u8], enemy = bb[t8], occ = own | enemy, empty = ~occ;
			ulong b, a;
			int from, to, pc;

			pc = u8 | PAWN;
			ulong pawns = bb[pc];
			int pcs = pc << 12;
			if (us == 0) {
				ulong p1 = (pawns << 8) & empty;
				b = p1 & RANK_8;
				while (b != 0) { to = BitOperations.TrailingZeroCount(b); b &= b - 1; ml[n++] = (to - 8) | (to << 6) | pcs | (1 << 20); }
				if (quiets) {
					b = p1 & ~RANK_8;
					while (b != 0) { to = BitOperations.TrailingZeroCount(b); b &= b - 1; ml[n++] = (to - 8) | (to << 6) | pcs; }
					b = ((p1 & RANK_3) << 8) & empty;
					while (b != 0) { to = BitOperations.TrailingZeroCount(b); b &= b - 1; ml[n++] = (to - 16) | (to << 6) | pcs; }
				}
				b = ((pawns & ~FILE_A) << 7) & enemy;
				while (b != 0) { to = BitOperations.TrailingZeroCount(b); b &= b - 1; ml[n++] = (to - 7) | (to << 6) | pcs | (sq[to] << 16) | (to >= 56 ? 1 << 20 : 0); }
				b = ((pawns & ~FILE_H) << 9) & enemy;
				while (b != 0) { to = BitOperations.TrailingZeroCount(b); b &= b - 1; ml[n++] = (to - 9) | (to << 6) | pcs | (sq[to] << 16) | (to >= 56 ? 1 << 20 : 0); }
			}
			else {
				ulong p1 = (pawns >> 8) & empty;
				b = p1 & RANK_1;
				while (b != 0) { to = BitOperations.TrailingZeroCount(b); b &= b - 1; ml[n++] = (to + 8) | (to << 6) | pcs | (1 << 20); }
				if (quiets) {
					b = p1 & ~RANK_1;
					while (b != 0) { to = BitOperations.TrailingZeroCount(b); b &= b - 1; ml[n++] = (to + 8) | (to << 6) | pcs; }
					b = ((p1 & RANK_6) >> 8) & empty;
					while (b != 0) { to = BitOperations.TrailingZeroCount(b); b &= b - 1; ml[n++] = (to + 16) | (to << 6) | pcs; }
				}
				b = ((pawns & ~FILE_A) >> 9) & enemy;
				while (b != 0) { to = BitOperations.TrailingZeroCount(b); b &= b - 1; ml[n++] = (to + 9) | (to << 6) | pcs | (sq[to] << 16) | (to < 8 ? 1 << 20 : 0); }
				b = ((pawns & ~FILE_H) >> 7) & enemy;
				while (b != 0) { to = BitOperations.TrailingZeroCount(b); b &= b - 1; ml[n++] = (to + 7) | (to << 6) | pcs | (sq[to] << 16) | (to < 8 ? 1 << 20 : 0); }
			}

			ulong targets = quiets ? ~own : enemy;

			pc = u8 | KNIGHT;
			pcs = pc << 12;
			b = bb[pc];
			while (b != 0) {
				from = BitOperations.TrailingZeroCount(b); b &= b - 1;
				a = knightAtt[from] & targets;
				while (a != 0) { to = BitOperations.TrailingZeroCount(a); a &= a - 1; ml[n++] = from | (to << 6) | pcs | (sq[to] << 16); }
			}
			pc = u8 | BISHOP;
			pcs = pc << 12;
			b = bb[pc];
			while (b != 0) {
				from = BitOperations.TrailingZeroCount(b); b &= b - 1;
				a = slide[bOff[from] + (int)(((occ & bMask[from]) * bMagic[from]) >> bShift[from])] & targets;
				while (a != 0) { to = BitOperations.TrailingZeroCount(a); a &= a - 1; ml[n++] = from | (to << 6) | pcs | (sq[to] << 16); }
			}
			pc = u8 | ROOK;
			pcs = pc << 12;
			b = bb[pc];
			while (b != 0) {
				from = BitOperations.TrailingZeroCount(b); b &= b - 1;
				a = slide[rOff[from] + (int)(((occ & rMask[from]) * rMagic[from]) >> rShift[from])] & targets;
				while (a != 0) { to = BitOperations.TrailingZeroCount(a); a &= a - 1; ml[n++] = from | (to << 6) | pcs | (sq[to] << 16); }
			}
			pc = u8 | QUEEN;
			pcs = pc << 12;
			b = bb[pc];
			while (b != 0) {
				from = BitOperations.TrailingZeroCount(b); b &= b - 1;
				a = (slide[bOff[from] + (int)(((occ & bMask[from]) * bMagic[from]) >> bShift[from])]
					| slide[rOff[from] + (int)(((occ & rMask[from]) * rMagic[from]) >> rShift[from])]) & targets;
				while (a != 0) { to = BitOperations.TrailingZeroCount(a); a &= a - 1; ml[n++] = from | (to << 6) | pcs | (sq[to] << 16); }
			}
			pc = u8 | KING;
			pcs = pc << 12;
			b = bb[pc];
			while (b != 0) {
				from = BitOperations.TrailingZeroCount(b); b &= b - 1;
				a = kingAtt[from] & targets;
				while (a != 0) { to = BitOperations.TrailingZeroCount(a); a &= a - 1; ml[n++] = from | (to << 6) | pcs | (sq[to] << 16); }
			}
			if (quiets && castle != 0) {
				if (us == 0) {
					if ((castle & 1) != 0 && sq[4] == KING) {
						if ((castle & 4) != 0 && sq[5] == 0 && sq[6] == 0 && sq[7] == ROOK) ml[n++] = 4 | (6 << 6) | (KING << 12) | (2 << 20);
						if ((castle & 2) != 0 && sq[3] == 0 && sq[2] == 0 && sq[1] == 0 && sq[0] == ROOK) ml[n++] = 4 | (2 << 6) | (KING << 12) | (2 << 20);
					}
				}
				else {
					if ((castle & 8) != 0 && sq[60] == (8 | KING)) {
						if ((castle & 32) != 0 && sq[61] == 0 && sq[62] == 0 && sq[63] == (8 | ROOK)) ml[n++] = 60 | (62 << 6) | ((8 | KING) << 12) | (2 << 20);
						if ((castle & 16) != 0 && sq[59] == 0 && sq[58] == 0 && sq[57] == 0 && sq[56] == (8 | ROOK)) ml[n++] = 60 | (58 << 6) | ((8 | KING) << 12) | (2 << 20);
					}
				}
			}
			return n - baseIdx;
		}

		/// <summary>True if a piece of color "by" attacks square s.</summary>
		private bool Attacked(int s, int by) {
			int b8 = by << 3;
			if ((pawnAtt[((by ^ 1) << 6) + s] & bb[b8 | PAWN]) != 0) return true;
			if ((knightAtt[s] & bb[b8 | KNIGHT]) != 0) return true;
			if ((kingAtt[s] & bb[b8 | KING]) != 0) return true;
			ulong occ = bb[0] | bb[8];
			ulong q = bb[b8 | QUEEN];
			ulong bq = bb[b8 | BISHOP] | q;
			if (bq != 0 && (slide[bOff[s] + (int)(((occ & bMask[s]) * bMagic[s]) >> bShift[s])] & bq) != 0) return true;
			ulong rq = bb[b8 | ROOK] | q;
			if (rq != 0 && (slide[rOff[s] + (int)(((occ & rMask[s]) * rMagic[s]) >> rShift[s])] & rq) != 0) return true;
			return false;
		}

		private bool InCheck(int color) {
			return Attacked(BitOperations.TrailingZeroCount(bb[(color << 3) | KING]), color ^ 1);
		}

		/// <summary>Static exchange evaluation: true if the move wins at least "threshold" material.</summary>
		private bool SeeGE(int m, int threshold) {
			if ((m >> 20) != 0) return threshold <= 0;
			int from = m & 63, to = (m >> 6) & 63;
			int swap = seeVal[(m >> 16) & 7] - threshold;
			if (swap < 0) return false;
			swap = seeVal[(m >> 12) & 7] - swap;
			if (swap <= 0) return true;
			ulong occ = (bb[0] | bb[8]) ^ (1UL << from) ^ (1UL << to);
			ulong bishops = bb[BISHOP] | bb[8 | BISHOP] | bb[QUEEN] | bb[8 | QUEEN];
			ulong rooks = bb[ROOK] | bb[8 | ROOK] | bb[QUEEN] | bb[8 | QUEEN];
			ulong attackers = (pawnAtt[64 + to] & bb[PAWN]) | (pawnAtt[to] & bb[8 | PAWN])
				| (knightAtt[to] & (bb[KNIGHT] | bb[8 | KNIGHT]))
				| (kingAtt[to] & (bb[KING] | bb[8 | KING]))
				| (slide[bOff[to] + (int)(((occ & bMask[to]) * bMagic[to]) >> bShift[to])] & bishops)
				| (slide[rOff[to] + (int)(((occ & rMask[to]) * rMagic[to]) >> rShift[to])] & rooks);
			int stm = side;
			int res = 1;
			while (true) {
				stm ^= 1;
				attackers &= occ;
				int s8 = stm << 3;
				ulong stmAtt = attackers & bb[s8];
				if (stmAtt == 0) break;
				res ^= 1;
				ulong pick;
				if ((pick = stmAtt & bb[s8 | PAWN]) != 0) {
					if ((swap = 100 - swap) < res) break;
					occ ^= pick & (0 - pick);
					attackers |= slide[bOff[to] + (int)(((occ & bMask[to]) * bMagic[to]) >> bShift[to])] & bishops;
				}
				else if ((pick = stmAtt & bb[s8 | KNIGHT]) != 0) {
					if ((swap = 320 - swap) < res) break;
					occ ^= pick & (0 - pick);
				}
				else if ((pick = stmAtt & bb[s8 | BISHOP]) != 0) {
					if ((swap = 330 - swap) < res) break;
					occ ^= pick & (0 - pick);
					attackers |= slide[bOff[to] + (int)(((occ & bMask[to]) * bMagic[to]) >> bShift[to])] & bishops;
				}
				else if ((pick = stmAtt & bb[s8 | ROOK]) != 0) {
					if ((swap = 500 - swap) < res) break;
					occ ^= pick & (0 - pick);
					attackers |= slide[rOff[to] + (int)(((occ & rMask[to]) * rMagic[to]) >> rShift[to])] & rooks;
				}
				else if ((pick = stmAtt & bb[s8 | QUEEN]) != 0) {
					if ((swap = 950 - swap) < res) break;
					occ ^= pick & (0 - pick);
					attackers |= (slide[bOff[to] + (int)(((occ & bMask[to]) * bMagic[to]) >> bShift[to])] & bishops)
						| (slide[rOff[to] + (int)(((occ & rMask[to]) * rMagic[to]) >> rShift[to])] & rooks);
				}
				else {
					// Only the king is left: it may capture only if the opponent has no attacker left.
					return ((attackers & bb[s8 ^ 8]) != 0 ? res ^ 1 : res) != 0;
				}
			}
			return res != 0;
		}

		// =====================================================================================
		// Evaluation (score from the side to move's point of view)
		// =====================================================================================

		private int Evaluate() {
			int ei = (int)(hash & (EC_SIZE - 1));
			if (ecKey[ei] == hash) return ecVal[ei];
			int v = EvaluateFull();
			ecKey[ei] = hash;
			ecVal[ei] = v;
			return v;
		}

		/// <summary>Pawn structure terms, cached by the pawn hash.</summary>
		private void EvalPawns(int pi) {
			int mg = 0, eg = 0;
			ulong passed = 0;
			for (int c = 0; c < 2; c++) {
				int c8 = c << 3;
				ulong own = bb[c8 | PAWN], opp = bb[(c8 ^ 8) | PAWN];
				ulong span;
				if (c == 0) {
					span = ((own & ~FILE_A) << 7) | ((own & ~FILE_H) << 9);
					span |= span << 8; span |= span << 16; span |= span << 32;
				}
				else {
					span = ((own & ~FILE_A) >> 9) | ((own & ~FILE_H) >> 7);
					span |= span >> 8; span |= span >> 16; span |= span >> 32;
				}
				phSpan[pi * 2 + c] = span;
				ulong phal = own & (((own << 1) & ~FILE_A) | ((own >> 1) & ~FILE_H));
				int cmg = 0, ceg = 0;
				ulong b = own;
				while (b != 0) {
					int s = BitOperations.TrailingZeroCount(b); b &= b - 1;
					int f = s & 7;
					int r = c == 0 ? s >> 3 : 7 - (s >> 3);
					int ci = (c << 6) + s;
					ulong sbit = 1UL << s;
					bool opposed = (frontFile[ci] & opp) != 0;
					bool doubled = (frontFile[ci] & own) != 0;
					int support = BitOperations.PopCount(pawnAtt[((c ^ 1) << 6) + s] & own);
					bool phalanx = (phal & sbit) != 0;
					if ((adjFiles[f] & own) == 0) {
						cmg -= 12; ceg -= 16;
						if (!opposed) cmg -= 8;
					}
					else if (support == 0 && !phalanx) {
						int stop = c == 0 ? s + 8 : s - 8;
						if (stop >= 0 && stop < 64 && (adjFiles[f] & own & ~forwardRanks[ci]) == 0
							&& ((pawnAtt[(c << 6) + stop] & opp) != 0 || (opp & (1UL << stop)) != 0)) {
							cmg -= 10; ceg -= 12;
							if (!opposed) cmg -= 8;
						}
					}
					if (doubled) { cmg -= 10; ceg -= 22; }
					if (support > 0 || phalanx) {
						int v = connectedBonus[r] * (phalanx ? 3 : 2) / (opposed ? 4 : 2) + 7 * support;
						cmg += v;
						ceg += v * (r > 3 ? r - 2 : 1) / 4;
					}
					if (!doubled && (passedMask[ci] & opp) == 0) {
						passed |= sbit;
						cmg += passedMg[r];
						ceg += passedEg[r];
					}
				}
				if (c == 0) { mg += cmg; eg += ceg; }
				else { mg -= cmg; eg -= ceg; }
			}
			phKey[pi] = pawnKey;
			phMg[pi] = mg;
			phEg[pi] = eg;
			phPassed[pi] = passed;
		}

		/// <summary>Pawn shelter and storm in front of a king standing on square k (positive = safe).</summary>
		private int Shelter(int c, int k) {
			int kf = k & 7, kr = k >> 3;
			int center = kf < 1 ? 1 : (kf > 6 ? 6 : kf);
			ulong ahead;
			if (c == 0) ahead = ~((1UL << (kr * 8)) - 1);
			else ahead = kr == 7 ? ~0UL : (1UL << ((kr + 1) * 8)) - 1;
			ulong own = bb[(c << 3) | PAWN] & ahead, opp = bb[((c ^ 1) << 3) | PAWN] & ahead;
			int score = 0;
			for (int f = center - 1; f <= center + 1; f++) {
				ulong fo = own & fileMask[f], fe = opp & fileMask[f];
				int ownR, oppR;
				if (c == 0) {
					ownR = fo != 0 ? BitOperations.TrailingZeroCount(fo) >> 3 : 0;
					oppR = fe != 0 ? BitOperations.TrailingZeroCount(fe) >> 3 : 0;
				}
				else {
					ownR = fo != 0 ? 7 - ((63 - BitOperations.LeadingZeroCount(fo)) >> 3) : 0;
					oppR = fe != 0 ? 7 - ((63 - BitOperations.LeadingZeroCount(fe)) >> 3) : 0;
				}
				score += shelterTab[ownR];
				if (oppR != 0) score += (ownR != 0 && oppR == ownR + 1) ? stormBlocked[oppR] : stormOpen[oppR];
				if (f == kf && fo == 0) score -= 12;
			}
			return score;
		}

		private int EvaluateFull() {
			ulong[] b = bb;
			ulong wP = b[PAWN], bP = b[8 | PAWN];
			ulong occ = b[0] | b[8];
			ulong minors = b[KNIGHT] | b[BISHOP] | b[8 | KNIGHT] | b[8 | BISHOP];
			if ((wP | bP | b[ROOK] | b[8 | ROOK] | b[QUEEN] | b[8 | QUEEN]) == 0 && (minors & (minors - 1)) == 0) return 0;

			int mg = psqMg, eg = psqEg;
			int pi = (int)(pawnKey & (PH_SIZE - 1));
			if (phKey[pi] != pawnKey) EvalPawns(pi);
			mg += phMg[pi];
			eg += phEg[pi];
			ulong passed = phPassed[pi];

			int wk = BitOperations.TrailingZeroCount(b[KING]), bk = BitOperations.TrailingZeroCount(b[8 | KING]);
			ulong[] at = attBy;
			ulong wPA = ((wP & ~FILE_A) << 7) | ((wP & ~FILE_H) << 9);
			ulong bPA = ((bP & ~FILE_A) >> 9) | ((bP & ~FILE_H) >> 7);
			at[PAWN] = wPA;
			at[8 | PAWN] = bPA;
			at[KNIGHT] = 0; at[BISHOP] = 0; at[ROOK] = 0; at[QUEEN] = 0;
			at[8 | KNIGHT] = 0; at[8 | BISHOP] = 0; at[8 | ROOK] = 0; at[8 | QUEEN] = 0;
			at[KING] = kingAtt[wk];
			at[8 | KING] = kingAtt[bk];
			at[0] = wPA | at[KING];
			at[8] = bPA | at[8 | KING];
			att2[0] = (wPA & at[KING]) | (((wP & ~FILE_A) << 7) & ((wP & ~FILE_H) << 9));
			att2[1] = (bPA & at[8 | KING]) | (((bP & ~FILE_A) >> 9) & ((bP & ~FILE_H) >> 7));

			ulong wZone = kingAtt[wk] | (1UL << wk);
			wZone |= wZone << 8;
			ulong bZone = kingAtt[bk] | (1UL << bk);
			bZone |= bZone >> 8;
			ulong allP = wP | bP;

			// ---- pieces: mobility, king attackers, outposts, files ----
			for (int c = 0; c < 2; c++) {
				int c8 = c << 3, e8 = c8 ^ 8;
				ulong ownP = b[c8 | PAWN], oppP = b[e8 | PAWN];
				ulong mobArea = ~(ownP | b[c8 | KING] | at[e8 | PAWN]);
				ulong eZone = c == 0 ? bZone : wZone;
				int ek = c == 0 ? bk : wk, ok = c == 0 ? wk : bk;
				ulong eAdj = kingAtt[ek];
				ulong outposts = (c == 0 ? WHITE_OUTPOST_RANKS : BLACK_OUTPOST_RANKS) & ~phSpan[pi * 2 + (c ^ 1)] & at[c8 | PAWN];
				int cmg = 0, ceg = 0, cnt = 0, wsum = 0, hits = 0;
				ulong pcs, a, sb;
				int s, mob;

				pcs = b[c8 | KNIGHT];
				while (pcs != 0) {
					s = BitOperations.TrailingZeroCount(pcs); pcs &= pcs - 1;
					a = knightAtt[s];
					at[c8 | KNIGHT] |= a; att2[c] |= at[c8] & a; at[c8] |= a;
					mob = BitOperations.PopCount(a & mobArea);
					cmg += nMobMg[mob]; ceg += nMobEg[mob];
					if ((a & eZone) != 0) { cnt++; wsum += KW_KNIGHT; hits += BitOperations.PopCount(a & eAdj); }
					sb = 1UL << s;
					if ((outposts & sb) != 0) { cmg += 34; ceg += 20; }
					else if ((a & outposts & ~b[c8]) != 0) { cmg += 14; ceg += 6; }
					if (((c == 0 ? sb << 8 : sb >> 8) & allP) != 0) cmg += 9;
				}

				pcs = b[c8 | BISHOP];
				while (pcs != 0) {
					s = BitOperations.TrailingZeroCount(pcs); pcs &= pcs - 1;
					a = slide[bOff[s] + (int)(((occ & bMask[s]) * bMagic[s]) >> bShift[s])];
					at[c8 | BISHOP] |= a; att2[c] |= at[c8] & a; at[c8] |= a;
					mob = BitOperations.PopCount(a & mobArea);
					cmg += bMobMg[mob]; ceg += bMobEg[mob];
					if ((a & eZone) != 0) { cnt++; wsum += KW_BISHOP; hits += BitOperations.PopCount(a & eAdj); }
					sb = 1UL << s;
					if ((outposts & sb) != 0) { cmg += 20; ceg += 8; }
					if (((c == 0 ? sb << 8 : sb >> 8) & allP) != 0) cmg += 9;
					int same = BitOperations.PopCount(ownP & ((LIGHT_SQUARES & sb) != 0 ? LIGHT_SQUARES : ~LIGHT_SQUARES));
					cmg -= 2 * same; ceg -= 5 * same;
					if (BitOperations.PopCount(slide[bOff[s] + (int)(((allP & bMask[s]) * bMagic[s]) >> bShift[s])] & CENTER4) > 1) cmg += 16;
				}
				if ((b[c8 | BISHOP] & (b[c8 | BISHOP] - 1)) != 0) { cmg += 28; ceg += 52; }

				pcs = b[c8 | ROOK];
				while (pcs != 0) {
					s = BitOperations.TrailingZeroCount(pcs); pcs &= pcs - 1;
					a = slide[rOff[s] + (int)(((occ & rMask[s]) * rMagic[s]) >> rShift[s])];
					at[c8 | ROOK] |= a; att2[c] |= at[c8] & a; at[c8] |= a;
					mob = BitOperations.PopCount(a & mobArea);
					cmg += rMobMg[mob]; ceg += rMobEg[mob];
					if ((a & eZone) != 0) { cnt++; wsum += KW_ROOK; hits += BitOperations.PopCount(a & eAdj); }
					int f = s & 7;
					if ((fileMask[f] & ownP) == 0) {
						if ((fileMask[f] & oppP) == 0) { cmg += 40; ceg += 18; }
						else { cmg += 18; ceg += 8; }
						int df = f - (ek & 7);
						if (df >= -1 && df <= 1) cmg += 10;
					}
					else if (mob <= 3) {
						int kf = ok & 7;
						if ((kf < 4) == (f < kf) && (castle & (c == 0 ? 1 : 8)) == 0) cmg -= 40;
					}
					int rr = c == 0 ? s >> 3 : 7 - (s >> 3);
					if (rr == 6) {
						int ekr = c == 0 ? ek >> 3 : 7 - (ek >> 3);
						if (ekr == 7 || (oppP & (c == 0 ? RANK_7 : RANK_2)) != 0) { cmg += 16; ceg += 30; }
					}
				}

				pcs = b[c8 | QUEEN];
				while (pcs != 0) {
					s = BitOperations.TrailingZeroCount(pcs); pcs &= pcs - 1;
					a = slide[bOff[s] + (int)(((occ & bMask[s]) * bMagic[s]) >> bShift[s])]
						| slide[rOff[s] + (int)(((occ & rMask[s]) * rMagic[s]) >> rShift[s])];
					at[c8 | QUEEN] |= a; att2[c] |= at[c8] & a; at[c8] |= a;
					mob = BitOperations.PopCount(a & mobArea);
					cmg += qMobMg[mob]; ceg += qMobEg[mob];
					if ((a & eZone) != 0) { cnt++; wsum += KW_QUEEN; hits += BitOperations.PopCount(a & eAdj); }
				}

				kAttCnt[c ^ 1] = cnt;
				kAttW[c ^ 1] = wsum;
				kAttHits[c ^ 1] = hits;
				if (c == 0) { mg += cmg; eg += ceg; }
				else { mg -= cmg; eg -= ceg; }
			}

			int npmW = 3 * BitOperations.PopCount(b[KNIGHT] | b[BISHOP]) + 5 * BitOperations.PopCount(b[ROOK]) + 9 * BitOperations.PopCount(b[QUEEN]);
			int npmB = 3 * BitOperations.PopCount(b[8 | KNIGHT] | b[8 | BISHOP]) + 5 * BitOperations.PopCount(b[8 | ROOK]) + 9 * BitOperations.PopCount(b[8 | QUEEN]);

			// ---- king safety: shelter, storm, attack units, safe checks ----
			for (int c = 0; c < 2; c++) {
				int c8 = c << 3, e8 = c8 ^ 8;
				int k = c == 0 ? wk : bk;
				int shelter = Shelter(c, k);
				if ((castle & (c == 0 ? 1 : 8)) != 0) {
					if ((castle & (c == 0 ? 4 : 32)) != 0) { int alt = Shelter(c, c == 0 ? 6 : 62) - 8; if (alt > shelter) shelter = alt; }
					if ((castle & (c == 0 ? 2 : 16)) != 0) { int alt = Shelter(c, c == 0 ? 2 : 58) - 8; if (alt > shelter) shelter = alt; }
				}
				int kmg = shelter, keg = 0;
				int cnt = kAttCnt[c];
				if (cnt > 0) {
					ulong weak = at[e8] & ~att2[c] & (~at[c8] | at[c8 | KING] | at[c8 | QUEEN]);
					ulong safe = ~b[e8] & (~at[c8] | (weak & att2[c ^ 1]));
					ulong rl = slide[rOff[k] + (int)(((occ & rMask[k]) * rMagic[k]) >> rShift[k])];
					ulong bl = slide[bOff[k] + (int)(((occ & bMask[k]) * bMagic[k]) >> bShift[k])];
					int danger = cnt * kAttW[c] + 55 * kAttHits[c]
						+ 140 * BitOperations.PopCount((c == 0 ? wZone : bZone) & weak)
						- 3 * shelter / 2 - 40;
					if ((rl & at[e8 | ROOK] & safe) != 0) danger += 880;
					if (((rl | bl) & at[e8 | QUEEN] & safe & ~at[c8 | QUEEN]) != 0) danger += 660;
					if ((bl & at[e8 | BISHOP] & safe) != 0) danger += 520;
					if ((knightAtt[k] & at[e8 | KNIGHT] & safe) != 0) danger += 720;
					if (b[e8 | QUEEN] == 0) danger -= 650;
					if (danger > 0) {
						int pen = danger * danger / 6144;
						kmg -= pen > 1000 ? 1000 : pen;
						keg -= danger / 20;
					}
				}
				if (c == 0) { mg += kmg; eg += keg; }
				else { mg -= kmg; eg -= keg; }
			}

			// ---- threats ----
			for (int c = 0; c < 2; c++) {
				int c8 = c << 3, e8 = c8 ^ 8;
				int tmg = 0, teg = 0, n;
				ulong nonPawn = b[e8] & ~b[e8 | PAWN] & ~b[e8 | KING];
				ulong strongly = at[e8 | PAWN] | (att2[c ^ 1] & ~att2[c]);
				ulong defended = nonPawn & strongly;
				ulong weak = b[e8] & ~b[e8 | KING] & ~strongly & at[c8];
				ulong t = at[c8 | PAWN] & nonPawn;
				if (t != 0) { n = BitOperations.PopCount(t); tmg += 58 * n; teg += 36 * n; }
				if ((defended | weak) != 0) {
					t = (defended | weak) & (at[c8 | KNIGHT] | at[c8 | BISHOP]);
					while (t != 0) {
						int s = BitOperations.TrailingZeroCount(t); t &= t - 1;
						int vt = sq[s] & 7;
						tmg += thrMinorMg[vt]; teg += thrMinorEg[vt];
					}
					t = weak & at[c8 | ROOK];
					while (t != 0) {
						int s = BitOperations.TrailingZeroCount(t); t &= t - 1;
						int vt = sq[s] & 7;
						tmg += thrRookMg[vt]; teg += thrRookEg[vt];
					}
					if ((weak & at[c8 | KING]) != 0) { tmg += 14; teg += 48; }
					t = weak & (~at[e8] | (nonPawn & att2[c]));
					if (t != 0) { n = BitOperations.PopCount(t); tmg += 40 * n; teg += 22 * n; }
				}
				ulong ownP = b[c8 | PAWN];
				ulong push, pt;
				if (c == 0) {
					push = (ownP << 8) & ~occ;
					push |= ((push & RANK_3) << 8) & ~occ;
				}
				else {
					push = (ownP >> 8) & ~occ;
					push |= ((push & RANK_6) >> 8) & ~occ;
				}
				push &= ~at[e8 | PAWN] & (at[c8] | ~at[e8]);
				pt = c == 0 ? (((push & ~FILE_A) << 7) | ((push & ~FILE_H) << 9)) : (((push & ~FILE_A) >> 9) | ((push & ~FILE_H) >> 7));
				pt &= nonPawn;
				if (pt != 0) { n = BitOperations.PopCount(pt); tmg += 30 * n; teg += 20 * n; }
				if (c == 0) { mg += tmg; eg += teg; }
				else { mg -= tmg; eg -= teg; }
			}

			// ---- passed pawns ----
			if (passed != 0) {
				for (int c = 0; c < 2; c++) {
					int c8 = c << 3, e8 = c8 ^ 8;
					ulong pp = passed & b[c8 | PAWN];
					if (pp == 0) continue;
					int ok = c == 0 ? wk : bk, ek = c == 0 ? bk : wk;
					int enemyNpm = c == 0 ? npmB : npmW;
					int pmg = 0, peg = 0;
					while (pp != 0) {
						int s = BitOperations.TrailingZeroCount(pp); pp &= pp - 1;
						int r = c == 0 ? s >> 3 : 7 - (s >> 3);
						int blk = c == 0 ? s + 8 : s - 8;
						if (enemyNpm == 0) {
							// Rule of the square in pawn endings.
							int promo = c == 0 ? 56 + (s & 7) : (s & 7);
							int pd = r == 1 ? 5 : 7 - r;
							int kd = dist[ek * 64 + promo] - (side == (c ^ 1) ? 1 : 0);
							if (kd > pd && (frontFile[(c << 6) + s] & b[c8]) == 0) peg += 420;
						}
						int w = passerWeight[r];
						if (w == 0) continue;
						int de = dist[ek * 64 + blk], dO = dist[ok * 64 + blk];
						peg += ((de < 5 ? de : 5) * 5 - (dO < 5 ? dO : 5) * 2) * w;
						if (r < 6) {
							int d2 = dist[ok * 64 + (c == 0 ? blk + 8 : blk - 8)];
							peg -= (d2 < 5 ? d2 : 5) * w;
						}
						if (sq[blk] == 0) {
							ulong path = frontFile[(c << 6) + s];
							ulong bad = path & (at[e8] | b[e8]);
							int k = bad == 0 ? 22 : ((bad & (1UL << blk)) == 0 ? 11 : 3);
							if ((at[c8] & (1UL << blk)) != 0) k += 4;
							pmg += k * w / 2;
							peg += k * w;
						}
					}
					if (c == 0) { mg += pmg; eg += peg; }
					else { mg -= pmg; eg -= peg; }
				}
			}

			// ---- space (opening / middlegame only) ----
			if (phase >= 16) {
				for (int c = 0; c < 2; c++) {
					int c8 = c << 3, e8 = c8 ^ 8;
					ulong ownP = b[c8 | PAWN];
					ulong safe = (c == 0 ? WHITE_SPACE : BLACK_SPACE) & ~ownP & ~at[e8 | PAWN];
					ulong behind = ownP;
					if (c == 0) { behind |= behind >> 8; behind |= behind >> 16; }
					else { behind |= behind << 8; behind |= behind << 16; }
					int bonus = BitOperations.PopCount(safe) + BitOperations.PopCount(behind & safe & ~at[e8]);
					int weight = BitOperations.PopCount(b[c8]) - 4;
					if (weight > 0) {
						int v = bonus * weight / 5;
						if (c == 0) mg += v; else mg -= v;
					}
				}
			}

			// ---- endgame scaling and mop-up ----
			int strong = eg > 0 ? 0 : 1;
			int scale = 64;
			int npmS = strong == 0 ? npmW : npmB, npmWk = strong == 0 ? npmB : npmW;
			ulong sPawns = b[(strong << 3) | PAWN], wkPawns = b[((strong ^ 1) << 3) | PAWN];
			if (sPawns == 0 && npmS - npmWk <= 3) {
				scale = npmS <= 3 ? 6 : 24;
			}
			else {
				ulong wb = b[BISHOP], bbish = b[8 | BISHOP];
				if (wb != 0 && bbish != 0 && (wb & (wb - 1)) == 0 && (bbish & (bbish - 1)) == 0
					&& ((wb & LIGHT_SQUARES) != 0) != ((bbish & LIGHT_SQUARES) != 0)) {
					scale = (npmW == 3 && npmB == 3) ? 26 : 48;
				}
			}
			if (wkPawns == 0 && npmS >= 5 && npmS - npmWk >= 2) {
				int sk = strong == 0 ? wk : bk, lk = strong == 0 ? bk : wk;
				int kd = Math.Abs((sk & 7) - (lk & 7)) + Math.Abs((sk >> 3) - (lk >> 3));
				int bonus = 14 * centerManhattan[lk] + 6 * (14 - kd);
				ulong sb = b[(strong << 3) | BISHOP], sn = b[(strong << 3) | KNIGHT];
				if (npmS == 6 && npmWk == 0 && sb != 0 && sn != 0) {
					bool light = (sb & LIGHT_SQUARES) != 0;
					int c1 = light ? 7 : 0, c2 = light ? 56 : 63;
					int d1 = Math.Abs((lk & 7) - (c1 & 7)) + Math.Abs((lk >> 3) - (c1 >> 3));
					int d2 = Math.Abs((lk & 7) - (c2 & 7)) + Math.Abs((lk >> 3) - (c2 >> 3));
					bonus += (14 - Math.Min(d1, d2)) * 12;
				}
				if (npmWk != 0) bonus /= 2;
				eg += strong == 0 ? bonus : -bonus;
			}

			int ph = phase < 24 ? phase : 24;
			int v2 = (mg * ph + eg * scale / 64 * (24 - ph)) / 24;
			return (side == 0 ? v2 : -v2) + TEMPO;
		}

		// =====================================================================================
		// Transposition table
		// =====================================================================================

		private void TTStore(int depth, int move, int score, int eval, int bound, int ply) {
			if (score >= MATE_BOUND) score += ply;
			else if (score <= -MATE_BOUND) score -= ply;
			int i = (int)(hash & (TT_SIZE - 2));
			int slot;
			if (tt[i].Key == hash) slot = i;
			else if (tt[i + 1].Key == hash) slot = i + 1;
			else if (tt[i].Age != ttAge || tt[i].Depth <= depth) slot = i;
			else slot = i + 1;
			if (tt[slot].Key == hash) {
				if (move == 0) move = tt[slot].Move;
				if (bound != BOUND_EXACT && tt[slot].Depth > depth + 2 && tt[slot].Age == ttAge) return;
			}
			tt[slot].Key = hash;
			tt[slot].Move = move;
			tt[slot].Score = (short)score;
			tt[slot].Eval = (short)eval;
			tt[slot].Depth = (byte)(depth < 0 ? 0 : depth > 250 ? 250 : depth);
			tt[slot].Bound = (byte)bound;
			tt[slot].Age = ttAge;
		}

		private int TTProbe() {
			int i = (int)(hash & (TT_SIZE - 2));
			if (tt[i].Key == hash) return i;
			if (tt[i + 1].Key == hash) return i + 1;
			return -1;
		}

		// =====================================================================================
		// Search
		// =====================================================================================

		private void CheckTime() {
			if (nodes >= nodeLimit || Stopwatch.GetTimestamp() >= deadline) stop = true;
		}

		private void ScoreMoves(int baseIdx, int n, int ttMove, int ply) {
			int[] ml = moveBuf, sc = scoreBuf;
			int k1 = killer1[ply], k2 = killer2[ply];
			int prev = ply > 0 ? playedMove[ply - 1] : 0;
			int cm = prev != 0 ? counterMove[((prev >> 12) & 15) * 64 + ((prev >> 6) & 63)] : 0;
			int prev2 = ply > 1 ? playedMove[ply - 2] : 0;
			int c1 = prev != 0 ? (((prev >> 12) & 15) * 64 + ((prev >> 6) & 63)) << 10 : -1;
			int c2 = prev2 != 0 ? (((prev2 >> 12) & 15) * 64 + ((prev2 >> 6) & 63)) << 10 : -1;
			int[] ch = contHist, hist = history;
			for (int j = baseIdx; j < baseIdx + n; j++) {
				int m = ml[j];
				int s;
				if (m == ttMove) s = SCORE_TT;
				else {
					int cap = (m >> 16) & 15;
					bool promo = (m >> 20) == 1;
					if (cap != 0 || promo) {
						int victim = seeVal[cap & 7];
						int mover = (m >> 12) & 7;
						int mvv = victim * 16 - mover + (promo ? 8000 : 0);
						if (promo || seeVal[mover] <= victim || SeeGE(m, 0)) s = SCORE_GOOD_CAPTURE + mvv;
						else s = SCORE_BAD_CAPTURE + mvv;
					}
					else if (m == k1) s = SCORE_KILLER1;
					else if (m == k2) s = SCORE_KILLER2;
					else if (m == cm) s = SCORE_COUNTER;
					else {
						int pt = ((m >> 12) & 15) * 64 + ((m >> 6) & 63);
						s = hist[pt];
						if (c1 >= 0) s += ch[c1 + pt];
						if (c2 >= 0) s += ch[c2 + pt];
					}
				}
				sc[j] = s;
			}
		}

		private static void Gravity(int[] table, int idx, int bonus) {
			int h = table[idx];
			table[idx] = h + bonus - h * Math.Abs(bonus) / 16384;
		}

		/// <summary>Rewards (bonus > 0) or penalizes a quiet move in the butterfly and continuation histories.</summary>
		private void UpdateQuietStats(int m, int bonus, int ply) {
			int pt = ((m >> 12) & 15) * 64 + ((m >> 6) & 63);
			Gravity(history, pt, bonus);
			int p1 = ply > 0 ? playedMove[ply - 1] : 0;
			if (p1 != 0) Gravity(contHist, ((((p1 >> 12) & 15) * 64 + ((p1 >> 6) & 63)) << 10) + pt, bonus);
			int p2 = ply > 1 ? playedMove[ply - 2] : 0;
			if (p2 != 0) Gravity(contHist, ((((p2 >> 12) & 15) * 64 + ((p2 >> 6) & 63)) << 10) + pt, bonus);
		}

		private int Search(int depth, int alpha, int beta, int ply, bool cutNode, bool nullOk) {
			bool pvNode = beta - alpha > 1;
			if ((++nodes & 255) == 0) CheckTime();
			if (stop) return 0;

			int hi = rootIdx + ply;
			repHash[hi] = hash;
			int us = side;
			if (ply > 0) {
				if (halfmove >= 100 || rootGamePly + ply >= 500) return 0;
				int lim = hi - halfmove;
				if (lim < 0) lim = 0;
				for (int j = hi - 4; j >= lim; j -= 2) {
					if (repHash[j] == hash) return 0;
				}
				if ((bb[PAWN] | bb[8 | PAWN] | bb[ROOK] | bb[8 | ROOK] | bb[QUEEN] | bb[8 | QUEEN]) == 0) {
					ulong mn = bb[KNIGHT] | bb[BISHOP] | bb[8 | KNIGHT] | bb[8 | BISHOP];
					if ((mn & (mn - 1)) == 0) return 0;
				}
				if (ply >= MAXPLY - 4) return Evaluate();
				int ma = -MATE + ply;
				if (alpha < ma) alpha = ma;
				int mb = MATE - ply - 1;
				if (beta > mb) beta = mb;
				if (alpha >= beta) return alpha;
			}

			bool inCheck = Attacked(BitOperations.TrailingZeroCount(bb[(us << 3) | KING]), us ^ 1);
			if (inCheck) depth++;
			if (depth <= 0) return QSearch(alpha, beta, ply);

			int ttMove = 0, ttScore = 0, ttBound = 0, ttDepth = -1, ttEval = 0;
			bool ttHit = false;
			int slot = TTProbe();
			if (slot >= 0) {
				ttHit = true;
				ttMove = tt[slot].Move;
				ttBound = tt[slot].Bound;
				ttDepth = tt[slot].Depth;
				ttEval = tt[slot].Eval;
				ttScore = tt[slot].Score;
				if (ttScore >= MATE_BOUND) ttScore -= ply;
				else if (ttScore <= -MATE_BOUND) ttScore += ply;
				if (!pvNode && ttDepth >= depth && halfmove < 90) {
					if (ttBound == BOUND_EXACT || (ttBound == BOUND_LOWER && ttScore >= beta) || (ttBound == BOUND_UPPER && ttScore <= alpha)) {
						return ttScore;
					}
				}
			}

			int eval, estimate;
			if (inCheck) {
				eval = -INF;
				estimate = -INF;
			}
			else {
				eval = ttHit ? ttEval : Evaluate();
				estimate = eval;
				if (ttHit && ttScore > -MATE_BOUND && ttScore < MATE_BOUND
					&& (ttBound == BOUND_EXACT || (ttBound == BOUND_LOWER && ttScore > eval) || (ttBound == BOUND_UPPER && ttScore < eval))) {
					estimate = ttScore;
				}
			}
			evalStack[ply] = eval;
			bool improving = !inCheck && (ply < 2 || evalStack[ply - 2] == -INF || eval > evalStack[ply - 2]);

			if (!pvNode && !inCheck) {
				// Reverse futility pruning.
				if (depth <= 8 && estimate < MATE_BOUND && estimate - (80 * depth - (improving ? 30 : 0)) >= beta) return estimate;
				// Razoring.
				if (depth <= 3 && estimate + 220 + 180 * depth <= alpha) {
					int v = QSearch(alpha, alpha + 1, ply);
					if (stop) return 0;
					if (v <= alpha) return v;
				}
				// Null move pruning (never with pawns and king only: zugzwang).
				if (nullOk && depth >= 3 && estimate >= beta && eval >= beta - 40 && beta > -MATE_BOUND
					&& (bb[(us << 3) | KNIGHT] | bb[(us << 3) | BISHOP] | bb[(us << 3) | ROOK] | bb[(us << 3) | QUEEN]) != 0) {
					int r = 3 + depth / 4 + Math.Min(3, (estimate - beta) / 200);
					playedMove[ply] = 0;
					MakeNull();
					int v = -Search(depth - 1 - r, -beta, -beta + 1, ply + 1, !cutNode, false);
					UnmakeNull();
					if (stop) return 0;
					if (v >= beta) {
						if (v >= MATE_BOUND) v = beta;
						if (depth < 12) return v;
						int vv = Search(depth - 1 - r, beta - 1, beta, ply, false, false);
						if (stop) return 0;
						if (vv >= beta) return v;
					}
				}
			}

			// Internal iterative reduction.
			if (ttMove == 0 && depth >= 4 && (pvNode || cutNode)) depth--;

			int baseIdx = ply * MOVES_PER_PLY;
			int n = GenMoves(baseIdx, true);
			ScoreMoves(baseIdx, n, ttMove, ply);
			int[] ml = moveBuf, sc = scoreBuf;
			int legal = 0, bestScore = -INF, bestMove = 0, quietCount = 0;
			int qBase = ply * 64;
			bool skipQuiets = false;
			int end = baseIdx + n;
			int lmpLimit = improving ? 3 + depth * depth : (3 + depth * depth) / 2;

			for (int i = baseIdx; i < end; i++) {
				int bi = i, bs = sc[i];
				for (int j = i + 1; j < end; j++) {
					if (sc[j] > bs) { bs = sc[j]; bi = j; }
				}
				int m = ml[bi];
				ml[bi] = ml[i]; ml[i] = m;
				sc[bi] = sc[i]; sc[i] = bs;

				bool quiet = ((m >> 16) & 15) == 0 && (m >> 20) != 1;
				if (quiet && skipQuiets) continue;
				bool badCapture = !quiet && bs < 0;

				if (!pvNode && !inCheck && bestScore > -MATE_BOUND && depth <= 5) {
					if (badCapture && !SeeGE(m, -100 * depth)) continue;
					if (quiet && depth <= 3 && bs < SCORE_COUNTER && (bs < -4096 * depth || !SeeGE(m, -60 * depth))) continue;
				}

				MakeMove(m);
				if (Attacked(BitOperations.TrailingZeroCount(bb[(us << 3) | KING]), us ^ 1)) {
					UnmakeMove(m);
					continue;
				}
				legal++;
				bool givesCheck = Attacked(BitOperations.TrailingZeroCount(bb[((us ^ 1) << 3) | KING]), us);

				if (quiet && !pvNode && !inCheck && !givesCheck && bestScore > -MATE_BOUND) {
					if (depth <= 8 && legal > lmpLimit) {
						UnmakeMove(m);
						skipQuiets = true;
						continue;
					}
					if (depth <= 7 && estimate + 100 + 90 * depth <= alpha && bs < SCORE_COUNTER) {
						UnmakeMove(m);
						continue;
					}
				}

				playedMove[ply] = m;
				int newDepth = depth - 1;
				int v;
				if (legal == 1) {
					v = -Search(newDepth, -beta, -alpha, ply + 1, !pvNode && !cutNode, true);
				}
				else {
					int r = 0;
					if (depth >= 3 && (quiet || badCapture)) {
						r = lmr[(depth < 63 ? depth : 63) * 64 + (legal < 63 ? legal : 63)];
						if (pvNode) r--;
						if (cutNode) r++;
						if (!improving) r++;
						if (givesCheck) r--;
						if (inCheck) r--;
						if (quiet) {
							if (bs >= SCORE_COUNTER) r--;
							else r -= bs / 8192;
						}
						if (r > newDepth - 1) r = newDepth - 1;
						if (r < 0) r = 0;
					}
					v = -Search(newDepth - r, -alpha - 1, -alpha, ply + 1, true, true);
					if (v > alpha && r > 0) v = -Search(newDepth, -alpha - 1, -alpha, ply + 1, !cutNode, true);
					if (v > alpha && v < beta && pvNode) v = -Search(newDepth, -beta, -alpha, ply + 1, false, true);
				}
				UnmakeMove(m);
				if (stop) return 0;

				if (v > bestScore) {
					bestScore = v;
					if (v > alpha) {
						bestMove = m;
						alpha = v;
						if (v >= beta) {
							if (quiet) {
								if (killer1[ply] != m) { killer2[ply] = killer1[ply]; killer1[ply] = m; }
								int prev = ply > 0 ? playedMove[ply - 1] : 0;
								if (prev != 0) counterMove[((prev >> 12) & 15) * 64 + ((prev >> 6) & 63)] = m;
								int bonus = 24 * depth * depth + 32 * depth;
								if (bonus > 1536) bonus = 1536;
								UpdateQuietStats(m, bonus, ply);
								for (int q = 0; q < quietCount; q++) UpdateQuietStats(quietBuf[qBase + q], -bonus, ply);
							}
							break;
						}
					}
				}
				if (quiet && quietCount < 64) quietBuf[qBase + quietCount++] = m;
			}

			if (legal == 0) {
				if (n == 0) return 0;   // no move at all: the arena calls it a draw
				// Every move loses the king. A draw rule may still trigger before the capture.
				if (halfmove >= 99 || rootGamePly + ply + 1 >= 500) return 0;
				return -MATE + ply;
			}

			int bound = bestScore >= beta ? BOUND_LOWER : (bestMove != 0 ? BOUND_EXACT : BOUND_UPPER);
			TTStore(depth, bestMove, bestScore, eval, bound, ply);
			return bestScore;
		}

		private int QSearch(int alpha, int beta, int ply) {
			if ((++nodes & 255) == 0) CheckTime();
			if (stop) return 0;
			if (ply >= MAXPLY - 2) return Evaluate();
			int us = side;
			bool inCheck = Attacked(BitOperations.TrailingZeroCount(bb[(us << 3) | KING]), us ^ 1);

			int ttMove = 0, ttEval = 0;
			bool ttHit = false;
			int slot = TTProbe();
			if (slot >= 0) {
				ttHit = true;
				ttMove = tt[slot].Move;
				ttEval = tt[slot].Eval;
				int ts = tt[slot].Score;
				if (ts >= MATE_BOUND) ts -= ply;
				else if (ts <= -MATE_BOUND) ts += ply;
				int tb = tt[slot].Bound;
				if (tb == BOUND_EXACT || (tb == BOUND_LOWER && ts >= beta) || (tb == BOUND_UPPER && ts <= alpha)) return ts;
			}

			int bestScore, standPat;
			if (inCheck) {
				standPat = -INF;
				bestScore = -INF;
			}
			else {
				standPat = ttHit ? ttEval : Evaluate();
				if (standPat >= beta) return standPat;
				if (standPat > alpha) alpha = standPat;
				bestScore = standPat;
			}

			int baseIdx = ply * MOVES_PER_PLY;
			int n = GenMoves(baseIdx, inCheck);
			int[] ml = moveBuf, sc = scoreBuf;
			int end = baseIdx + n;
			for (int j = baseIdx; j < end; j++) {
				int m = ml[j];
				int cap = (m >> 16) & 15;
				if (m == ttMove) sc[j] = SCORE_TT;
				else if (cap != 0 || (m >> 20) == 1) sc[j] = SCORE_GOOD_CAPTURE + seeVal[cap & 7] * 16 - ((m >> 12) & 7) + ((m >> 20) == 1 ? 8000 : 0);
				else sc[j] = history[((m >> 12) & 15) * 64 + ((m >> 6) & 63)];
			}
			int legal = 0, bestMove = 0, origAlpha = alpha;
			for (int i = baseIdx; i < end; i++) {
				int bi = i, bs = sc[i];
				for (int j = i + 1; j < end; j++) {
					if (sc[j] > bs) { bs = sc[j]; bi = j; }
				}
				int m = ml[bi];
				ml[bi] = ml[i]; ml[i] = m;
				sc[bi] = sc[i]; sc[i] = bs;
				if (!inCheck) {
					int cap = (m >> 16) & 7;
					if ((m >> 20) != 1) {
						if (standPat + seeVal[cap] + 180 <= alpha) continue;
						if (seeVal[(m >> 12) & 7] > seeVal[cap] && !SeeGE(m, 0)) continue;
					}
				}
				MakeMove(m);
				if (Attacked(BitOperations.TrailingZeroCount(bb[(us << 3) | KING]), us ^ 1)) {
					UnmakeMove(m);
					continue;
				}
				legal++;
				int v = -QSearch(-beta, -alpha, ply + 1);
				UnmakeMove(m);
				if (stop) return 0;
				if (v > bestScore) {
					bestScore = v;
					if (v > alpha) {
						alpha = v;
						bestMove = m;
						if (v >= beta) break;
					}
				}
			}
			if (inCheck && legal == 0) {
				if (n == 0) return 0;
				return -MATE + ply;
			}
			int bound = bestScore >= beta ? BOUND_LOWER : (alpha > origAlpha ? BOUND_EXACT : BOUND_UPPER);
			TTStore(0, bestMove, bestScore, inCheck ? -INF : standPat, bound, ply);
			return bestScore;
		}

		private int SearchRoot(int depth, int alpha, int beta) {
			int bestScore = -INF;
			int us = side;
			bool inCheck = Attacked(BitOperations.TrailingZeroCount(bb[(us << 3) | KING]), us ^ 1);
			repHash[rootIdx] = hash;
			evalStack[0] = inCheck ? -INF : Evaluate();
			for (int i = 0; i < rootCount; i++) {
				int m = rootMoves[i];
				long before = nodes;
				MakeMove(m);
				playedMove[0] = m;
				bool givesCheck = Attacked(BitOperations.TrailingZeroCount(bb[((us ^ 1) << 3) | KING]), us);
				bool quiet = ((m >> 16) & 15) == 0 && (m >> 20) != 1;
				int v;
				if (i == 0) {
					v = -Search(depth - 1, -beta, -alpha, 1, false, true);
				}
				else {
					int r = 0;
					if (depth >= 3 && i >= 3 && quiet && !givesCheck && !inCheck) {
						r = lmr[(depth < 63 ? depth : 63) * 64 + (i < 63 ? i : 63)] - 1;
						if (r > depth - 2) r = depth - 2;
						if (r < 0) r = 0;
					}
					v = -Search(depth - 1 - r, -alpha - 1, -alpha, 1, true, true);
					if (v > alpha && r > 0) v = -Search(depth - 1, -alpha - 1, -alpha, 1, true, true);
					if (v > alpha && v < beta) v = -Search(depth - 1, -beta, -alpha, 1, false, true);
				}
				UnmakeMove(m);
				rootNodes[i] += nodes - before;
				if (stop) break;
				if (v > bestScore) bestScore = v;
				if (v > alpha) {
					alpha = v;
					rootBest = m;
					long rn = rootNodes[i];
					for (int j = i; j > 0; j--) {
						rootMoves[j] = rootMoves[j - 1];
						rootNodes[j] = rootNodes[j - 1];
					}
					rootMoves[0] = m;
					rootNodes[0] = rn;
					if (v >= beta) break;
				}
			}
			return bestScore;
		}

		/// <summary>Iterative deepening on the internal board. Returns the best legal move, or 0 if there is none.</summary>
		private int SearchPosition(long hardTicks, long softTicks, long maxNodes) {
			deadline = hardTicks;
			nodeLimit = maxNodes;
			stop = false;
			nodes = 0;
			searchFailed = false;
			ttAge++;
			Array.Clear(killer1, 0, killer1.Length);
			Array.Clear(killer2, 0, killer2.Length);
			for (int i = 0; i < history.Length; i++) history[i] /= 2;

			int us = side;
			int n = GenMoves(0, true);
			int slot = TTProbe();
			ScoreMoves(0, n, slot >= 0 ? tt[slot].Move : 0, 0);
			rootCount = 0;
			for (int i = 0; i < n; i++) {
				int m = moveBuf[i];
				MakeMove(m);
				bool ok = !Attacked(BitOperations.TrailingZeroCount(bb[(us << 3) | KING]), us ^ 1);
				UnmakeMove(m);
				if (!ok) continue;
				// insertion by score (descending)
				int s = scoreBuf[i];
				int j = rootCount++;
				while (j > 0 && scoreBuf[MOVES_PER_PLY * 2 + j - 1] < s) {
					rootMoves[j] = rootMoves[j - 1];
					scoreBuf[MOVES_PER_PLY * 2 + j] = scoreBuf[MOVES_PER_PLY * 2 + j - 1];
					j--;
				}
				rootMoves[j] = m;
				scoreBuf[MOVES_PER_PLY * 2 + j] = s;
			}
			if (rootCount == 0) return 0;
			rootBest = rootMoves[0];
			if (rootCount == 1) return rootBest;

			try {
				int prevScore = 0;
				for (int d = 1; d <= MAXDEPTH; d++) {
					for (int i = 0; i < rootCount; i++) rootNodes[i] = 0;
					int delta = 20, alpha = -INF, beta = INF, score;
					if (d >= 4) {
						alpha = Math.Max(-INF, prevScore - delta);
						beta = Math.Min(INF, prevScore + delta);
					}
					while (true) {
						score = SearchRoot(d, alpha, beta);
						if (stop) break;
						if (score <= alpha) {
							beta = (alpha + beta) / 2;
							alpha = Math.Max(-INF, score - delta);
						}
						else if (score >= beta) {
							beta = Math.Min(INF, score + delta);
						}
						else break;
						delta += delta / 2 + 10;
						if (delta > 700) { alpha = -INF; beta = INF; }
					}
					if (stop) break;
					prevScore = score;
					// Order the remaining root moves by the effort they needed (best move stays first).
					for (int i = 2; i < rootCount; i++) {
						int m = rootMoves[i];
						long rn = rootNodes[i];
						int j = i;
						while (j > 1 && rootNodes[j - 1] < rn) {
							rootMoves[j] = rootMoves[j - 1];
							rootNodes[j] = rootNodes[j - 1];
							j--;
						}
						rootMoves[j] = m;
						rootNodes[j] = rn;
					}
					if (Stopwatch.GetTimestamp() >= softTicks) break;
					if (score >= MATE_BOUND && d >= MATE - score + 2) break;
				}
			}
			catch (Exception) {
				searchFailed = true;
			}
			return rootBest;
		}

		// =====================================================================================
		// Bot interface
		// =====================================================================================

		public Move Think(BotBoard board, BotTimer timer) {
			List<Move> api = null;
			Move chosen = null;
			try {
				api = board.GetLegalMoves();
				if (api == null || api.Count == 0) return null;
				for (int i = 0; i < api.Count; i++) {
					Piece cp = api[i].CapturedPiece;
					if (cp != null && cp.Type == PieceType.King) return api[i];
				}
				chosen = api[0];

				// The deadlines are taken from the timer after all preparation, so every cost before the search counts.
				int limit = timer.TimeLimitMilliseconds;
				long margin = Math.Min(300, Math.Max(10, limit / 10));
				long freq = Stopwatch.Frequency;
				long budget;
				if (engineOk && ReadBoard(board) && VerifyRoot(api)) {
					TrackHistory(board.PlyCount);
					long now = Stopwatch.GetTimestamp();
					budget = Math.Max(1, limit - margin - timer.ElapsedMilliseconds);
					int best = SearchPosition(now + budget * freq / 1000, now + budget * 65 / 100 * freq / 1000, long.MaxValue);
					if (best != 0) {
						Move mapped = FindApiMove(api, best);
						if (mapped != null) {
							if (searchFailed) histValid = false;
							else RecordMyMove(best);
							return mapped;
						}
					}
				}
				histValid = false;
				budget = Math.Max(1, limit - margin - timer.ElapsedMilliseconds);
				Move fb = FallbackSearch(board, api, Stopwatch.GetTimestamp() + budget * freq / 1000);
				if (fb != null) chosen = fb;
			}
			catch (Exception) {
				histValid = false;
			}
			if (chosen == null && api != null && api.Count > 0) chosen = api[0];
			return chosen;
		}

		private bool ReadBoard(BotBoard board) {
			if (board.Width != 8 || board.Height != 8) return false;
			Array.Clear(bb, 0, bb.Length);
			Array.Clear(sq, 0, sq.Length);
			castle = 0;
			int wKings = 0, bKings = 0;
			for (int y = 0; y < 8; y++) {
				for (int x = 0; x < 8; x++) {
					if (!board.IsOnBoard(new Vector2Int(x, y))) return false;
					Piece p = board.GetPiece(x, y);
					if (p == null || p.Type == PieceType.None) continue;
					int c;
					if (p.Team == TeamType.White) c = 0;
					else if (p.Team == TeamType.Black) c = 1;
					else return false;
					int t;
					switch (p.Type) {
						case PieceType.Pawn: t = PAWN; break;
						case PieceType.Knight: t = KNIGHT; break;
						case PieceType.Bishop: t = BISHOP; break;
						case PieceType.Rook: t = ROOK; break;
						case PieceType.Queen: t = QUEEN; break;
						case PieceType.King: t = KING; break;
						default: return false;
					}
					int s = y * 8 + x;
					Put(s, (c << 3) | t);
					if (t == KING) {
						if (c == 0) wKings++; else bKings++;
					}
					if (p.MoveCount == 0) {
						if (t == KING && c == 0 && s == 4) castle |= 1;
						if (t == KING && c == 1 && s == 60) castle |= 8;
						if (t == ROOK && c == 0 && s == 0) castle |= 2;
						if (t == ROOK && c == 0 && s == 7) castle |= 4;
						if (t == ROOK && c == 1 && s == 56) castle |= 16;
						if (t == ROOK && c == 1 && s == 63) castle |= 32;
					}
				}
			}
			if (wKings != 1 || bKings != 1) return false;
			TeamType stm = board.SideToMove;
			if (stm == TeamType.White) side = 0;
			else if (stm == TeamType.Black) side = 1;
			else return false;
			sp = 0;
			halfmove = 0;
			ComputeKeys();
			return true;
		}

		/// <summary>The internal pseudo-legal move list must match the API's move list exactly (by from/to).</summary>
		private bool VerifyRoot(List<Move> api) {
			int n = GenMoves(0, true);
			if (n != api.Count || n > MOVES_PER_PLY) return false;
			for (int i = 0; i < n; i++) {
				verifyA[i] = moveBuf[i] & 4095;
				Move mv = api[i];
				Vector2Int f = mv.From, t = mv.To;
				if (f.x < 0 || f.x > 7 || f.y < 0 || f.y > 7 || t.x < 0 || t.x > 7 || t.y < 0 || t.y > 7) return false;
				verifyB[i] = (f.y * 8 + f.x) | ((t.y * 8 + t.x) << 6);
			}
			Array.Sort(verifyA, 0, n);
			Array.Sort(verifyB, 0, n);
			for (int i = 0; i < n; i++) {
				if (verifyA[i] != verifyB[i]) return false;
			}
			return true;
		}

		private static Move FindApiMove(List<Move> api, int m) {
			int from = m & 63, to = (m >> 6) & 63;
			for (int i = 0; i < api.Count; i++) {
				Vector2Int f = api[i].From, t = api[i].To;
				if (f.y * 8 + f.x == from && t.y * 8 + t.x == to) return api[i];
			}
			return null;
		}

		/// <summary>Maintains the positions of this game (for repetitions) and the no-progress clock.</summary>
		private void TrackHistory(int plyCount) {
			if (histValid && plyCount == lastPly + 2) {
				AppendHistory(lastRootHash);
				AppendHistory(lastAfterHash);
				bool progress = BitOperations.PopCount(bb[0] | bb[8]) < lastAfterPieceCount || (bb[PAWN] | bb[8 | PAWN]) != lastAfterPawns;
				halfmove = progress ? 0 : lastAfterHalf + 1;
			}
			else {
				gameHistCount = 0;
				halfmove = 0;
				if (plyCount == 1) AppendHistory(startHash);
			}
			histValid = true;
			lastPly = plyCount;
			lastRootHash = hash;
			rootIdx = gameHistCount;
			rootGamePly = plyCount;
		}

		private void AppendHistory(ulong h) {
			if (gameHistCount >= HIST_SIZE - 4) {
				// Only the last 100 plies can ever repeat; keep a comfortable tail.
				int keep = 200;
				Array.Copy(repHash, gameHistCount - keep, repHash, 0, keep);
				gameHistCount = keep;
			}
			repHash[gameHistCount++] = h;
		}

		private void RecordMyMove(int m) {
			MakeMove(m);
			lastAfterHash = hash;
			lastAfterHalf = halfmove;
			lastAfterPieceCount = BitOperations.PopCount(bb[0] | bb[8]);
			lastAfterPawns = bb[PAWN] | bb[8 | PAWN];
			UnmakeMove(m);
		}

		/// <summary>Simple and slow search on the API board, only used if the internal engine cannot be trusted.</summary>
		private static Move FallbackSearch(BotBoard board, List<Move> moves, long hardTicks) {
			TeamType me = board.SideToMove;
			TeamType opp = me == TeamType.White ? TeamType.Black : TeamType.White;
			Move best = null;
			int bestScore = int.MinValue;
			foreach (Move m in moves) {
				if (best != null && Stopwatch.GetTimestamp() >= hardTicks) break;
				board.MakeMove(m);
				int score;
				try {
					if (board.IsKingAttacked(me)) score = -1000000;
					else {
						score = 0;
						foreach (PieceOnSquare p in board.GetAllPieces()) {
							int v = PieceValue(p.Type);
							score += p.Team == me ? v : -v;
						}
						score *= 10;
						int worst = 0;
						foreach (Move r in board.GetLegalMoves()) {
							if (!r.IsCapture) continue;
							int v = PieceValue(r.CapturedPiece.Type);
							if (v > worst) worst = v;
						}
						score -= worst * 8;
						if (board.IsKingAttacked(opp)) score += 5;
					}
				}
				finally {
					board.UndoMove();
				}
				if (score > bestScore) {
					bestScore = score;
					best = m;
				}
			}
			return best;
		}

		private static int PieceValue(PieceType t) {
			switch (t) {
				case PieceType.Pawn: return 100;
				case PieceType.Knight: return 320;
				case PieceType.Bishop: return 330;
				case PieceType.Rook: return 500;
				case PieceType.Queen: return 950;
				case PieceType.King: return 100000;
				default: return 0;
			}
		}

		// =====================================================================================
		// Debug helpers used by the test harness (perft against the engine's move generator)
		// =====================================================================================

		public long DebugPerft(BotBoard board, int depth) {
			if (!ReadBoard(board)) return -1;
			return Perft(depth, 0);
		}

		private long Perft(int depth, int ply) {
			if (depth == 0) return 1;
			if (bb[KING] == 0 || bb[8 | KING] == 0) return 1;
			int baseIdx = ply * MOVES_PER_PLY;
			int n = GenMoves(baseIdx, true);
			if (depth == 1) return n;
			long total = 0;
			for (int i = 0; i < n; i++) {
				int m = moveBuf[baseIdx + i];
				MakeMove(m);
				total += Perft(depth - 1, ply + 1);
				UnmakeMove(m);
			}
			return total;
		}

		public List<string> DebugMoves(BotBoard board) {
			var list = new List<string>();
			if (!ReadBoard(board)) return list;
			int n = GenMoves(0, true);
			for (int i = 0; i < n; i++) {
				int m = moveBuf[i];
				list.Add(SquareName(m & 63) + SquareName((m >> 6) & 63));
			}
			return list;
		}

		private static string SquareName(int s) {
			return ((char)('a' + (s & 7))).ToString() + (char)('1' + (s >> 3));
		}
	}
}
