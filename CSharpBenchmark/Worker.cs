using System;
using System.Diagnostics;

namespace CSharpBenchmark
{
	public class Worker
	{
		private int id = -1;
		public static int engine = configuration_data.engine;
		// global constant variables
		private static readonly double g0 = 32.2;
		private static readonly double gama = 1.4;
		private static readonly double tt4 = 2500.0;
		private static readonly double tt7 = 2500.0;
		private static readonly double p3p2d = 8.0;
		private static readonly double p3fp2d = 2.0;
		private static readonly double byprat = 1.0;
		private static readonly double fhv = 18600.0;
		private static readonly double acore = 2.0;
		private static readonly double afan = 2.0;
		private static readonly double dfan = 293.02;
		private static readonly double dcomp = 293.02;
		private static readonly double dburner = 515.2;
		private static readonly double dturbin = 515.2;
		private static readonly double dnozl = 515.2;
		// local variables for calculating
		double altd, u0d;
		double throtl;
		double[] trat = new double[20];
		double[] tt = new double[20];
		double[] prat = new double[20];
		double[] pt = new double[20];
		double[] eta = new double[20];
		double[] gam = new double[20];
		double[] cp = new double[20];
		double weight;
		double Rgas, alt, ts0, ps0, a0, u0, fsmach, q0, tsout, psout, rho0, cpair;
		double a8, a8d, a4, m2, mfr, isp;
		double epr, etr, npr, snpr;
		double fnet, fgros, dram, sfc, fa, eair, uexit, ues;
		double fnlb, fglb, drlb, flflo;
		double pexit, pfexit;
		double a8max, a8rat, a4p;
		double lcomp, lburn, lturb, lnoz, ncomp, nturb;
		double sblade, hblade, tblade, xcomp, ncompd;

		public Worker (int id)
		{
			this.id = id;
			InitializeParam ();
		}

		public void run()
		{
			// variables for calculating time related, e.g.deadline etc.
			/* for each thread */
			double TimePoint = 0;
			double ExecTime = 0;
			double used = 0, ExecTotTime = 0, usedTime = 0;
			double PiTime;
			// variables for pi calculation
			const long num_steps = 1000000;
			double step = 1.0 / (double) num_steps;
			int i = 0;
			double x, pi, sum;
			// variables for input data
			double a, b, c, d;
			while (true) {	

				if (rtPerformancedata.getNumPoints() < mpbenchmark.LineCount) {
					int CurrentPoint = rtPerformancedata.nextNumPoints();
					if (CurrentPoint > mpbenchmark.LineCount) {
						rtPerformancedata.undonextNumPoints ();
						break;
					}
					Stopwatch calwatch = new Stopwatch();
					Stopwatch piwatch = new Stopwatch();

					/** Pi calculation */
					sum = 0;
					piwatch.Start ();
					for (i = 0; i < num_steps; i++) {
						x = (i + 0.5) * step;
						sum += 4.0 / (1.0 + x * x);
					}
					{
						pi = sum * step;
					}
					piwatch.Stop();
					PiTime = piwatch.Elapsed.TotalSeconds;

					/** read input data ---Speed Altitude and Throttle */
					int index = CurrentPoint - 1;
					a = mpbenchmark.inputArray[index,0];
					b = mpbenchmark.inputArray[index,1];
					c = mpbenchmark.inputArray[index,2];
					d = mpbenchmark.inputArray[index,3];
					if (a < 0 || a > 1500) {
						Console.Write(String.Format("Warning : incorrect speed for point {0:d}\n", CurrentPoint));
						u0d = 0;
					} else
						// Input speed in mph
						u0d = a;

					if (b < 0 || b > 50000) {
						Console.Write(String.Format("Warning : incorrect altitude for point {0:d}\n", CurrentPoint));
						altd = 0;
					} else
						// Input altitude in feet
						altd = b;

					if (c < 45 || c > 90) {
						Console.Write(String.Format("Warning : incorrect throttle for point {0:d}\n", CurrentPoint));
						throtl = 100;
					} else
						// Converting input throttle in %
						throtl = Utilities.deg2rad(c, pi) * 100 * 2 / pi;

					if (d < 0) {
						Console.Write(String.Format("Warning : incorrect deadline for point {0:d}\n", CurrentPoint));
						TimePoint = 0;
					} else {
						// Input time point
						TimePoint = d;
					}
					// TotalTimePoint += TimePoint;
					rtPerformancedata.increaseTotalTimePoint(TimePoint);

					/** ******** START CALCULATIONS **********/
					calwatch.Start ();
					deduceInputs();
					getThermo();
					getGeo();
					calcPerf();
					calwatch.Stop ();

					ExecTime = calwatch.Elapsed.TotalSeconds;
					used = (ExecTime + PiTime) / TimePoint;
					ExecTotTime = ExecTime + PiTime;
					usedTime = (ExecTime + PiTime) - TimePoint;
					// TotalTime += ExecTotTime;
					// TotalUsed += used;
					rtPerformancedata.increaseTotalTime(ExecTotTime);
					rtPerformancedata.increaseTotalUsed(used);

					/*********** PRINT RESULTS ************/
					if (used > 1) {
						// NumMissed ++;
						// TotalTimePoint += usedTime;
						rtPerformancedata.increaseNumMissed(1);
						rtPerformancedata.increaseTotalTimePoint(usedTime);
					}
					mpbenchmark.outputArray[index,0] = id;
					mpbenchmark.outputArray[index,1] = ExecTotTime;
					mpbenchmark.outputArray[index,2] = u0d;
					mpbenchmark.outputArray[index,3] = altd;
					mpbenchmark.outputArray[index,4] = throtl;
					mpbenchmark.outputArray[index,5] = fsmach;
					mpbenchmark.outputArray[index,6] = psout;
					mpbenchmark.outputArray[index,7] = tsout;
					mpbenchmark.outputArray[index,8] = fnlb;
					mpbenchmark.outputArray[index,9] = fglb;
					mpbenchmark.outputArray[index,10] = drlb;
					mpbenchmark.outputArray[index,11] = flflo;
					mpbenchmark.outputArray[index,12] = sfc;
					mpbenchmark.outputArray[index,13] = eair;
					mpbenchmark.outputArray[index,14] = weight;
					mpbenchmark.outputArray[index,15] = fnlb / weight;
					mpbenchmark.outputArray[index,16] = used * 100;
					mpbenchmark.outputArray[index,17] = CurrentPoint;
				}// end of if
				else {
					break;
				}
			}// end of while
		}

		// Initialize parameters
		private void InitializeParam() {
			prat[4] = 1.0;
			eta[13] = 1.0;
			eta[4] = 1.0;
			eta[7] = 1.0;
			eta[3] = 1.0;
			eta[5] = 1.0;
			a4 = 0.418;
			a8max = 0.4;
		}
		public void deduceInputs() {
			Rgas = 1718.0; /* ft2/sec2 R */

			alt = altd;
			if (alt < 36152.0) {
				ts0 = 518.6 - 3.56 * alt / 1000.0;
				ps0 = 2116.0 * Utilities.fpow(ts0 / 518.6, 5.256);
			}
			if (alt >= 36152.0 && alt <= 82345.0) { // Stratosphere
				ts0 = 389.98;
				ps0 = 2116.0 * 0.2236 * Utilities.expo((36000.0 - alt) / (53.35 * 389.98));
			}
			if (alt >= 82345.0) {
				ts0 = 389.98 + 1.645 * (alt - 82345) / 1000.0;
				ps0 = 2116.0 * 0.02456 * Utilities.fpow(ts0 / 389.98, -11.388);
			}

			a0 = Utilities.sqroot(gama * Rgas * ts0); // speed of sound ft/sec

			u0 = u0d * 5280.0 / 3600.0; // airspeed ft/sec
			fsmach = u0 / a0;
			q0 = gama / 2.0 * fsmach * fsmach * ps0;

			if (u0 > .0001)
				rho0 = q0 / (u0 * u0);
			else
				rho0 = 1.0;

			tt[0] = ts0 * (1.0 + .5 * (gama - 1.0) * fsmach * fsmach);
			pt[0] = ps0 * Utilities.fpow(tt[0] / ts0, gama / (gama - 1.0));
			ps0 = ps0 / 144.0;
			pt[0] = pt[0] / 144.0;
			cpair = Utilities.getCp(tt[0]); // BTU/lbm R
			tsout = ts0 - 459.6;
			psout = ps0;
		}

		// Utility to have Thermodynamic parameters
		public void getThermo() {
			double delhc, delhht, delhf, delhlt;
			double deltc, deltht, deltf, deltlt;

			// inlet recovery
			if (fsmach > 1.0) // supersonic
			{
				prat[2] = 1.0 - .075 * Utilities.fpow(fsmach - 1.0, 1.35);
			} else {
				prat[2] = 1.0;
			}
			eta[2] = prat[2];

			// protection for overwriting input
			if (eta[3] < .5)
				eta[3] = .5;
			if (eta[5] < .5)
				eta[5] = .5;
			trat[7] = 1.0;
			prat[7] = 1.0;
			tt[2] = tt[1] = tt[0];
			pt[1] = pt[0];
			gam[2] = Utilities.getGama(tt[2]);
			cp[2] = Utilities.getCp(tt[2]);
			pt[2] = pt[1] * prat[2];

			// design - p3p2 specified - tt4 specified
			if (engine <= 2) // turbojet
			{
				prat[3] = p3p2d; // core compressor
				if (prat[3] < .5)
					prat[3] = .5;

				delhc = (cp[2] * tt[2] / eta[3]) * (Utilities.fpow(prat[3], (gam[2] - 1.0) / gam[2]) - 1.0); // 0.25
				deltc = delhc / cp[2];
				pt[3] = pt[2] * prat[3];
				tt[3] = tt[2] + deltc;
				trat[3] = tt[3] / tt[2];
				gam[3] = Utilities.getGama(tt[3]);
				cp[3] = Utilities.getCp(tt[3]);
				tt[4] = tt4 * throtl / 100.0;
				gam[4] = Utilities.getGama(tt[4]);
				cp[4] = Utilities.getCp(tt[4]);
				trat[4] = tt[4] / tt[3];
				pt[4] = pt[3] * prat[4];
				delhht = delhc;
				deltht = delhht / cp[4];
				tt[5] = tt[4] - deltht;
				gam[5] = Utilities.getGama(tt[5]);
				cp[5] = Utilities.getCp(tt[5]);
				trat[5] = tt[5] / tt[4];
				prat[5] = Utilities.fpow((1 - delhht / cp[4] / tt[4] / eta[5]), (gam[4] / (gam[4] - 1.0)));
				pt[5] = pt[4] * prat[5];

				// fan conditions
				prat[13] = 1.0;
				trat[13] = 1.0;
				tt[13] = tt[2];
				pt[13] = pt[2];
				gam[13] = gam[2];
				cp[13] = cp[2];
				prat[15] = 1.0;
				pt[15] = pt[5];
				trat[15] = 1.0;
				tt[15] = tt[5];
				gam[15] = gam[5];
				cp[15] = cp[5];
			}

			if (engine == 3) // turbofan
			{
				prat[13] = p3fp2d;
				if (prat[13] < .5)
					prat[13] = .5;

				delhf = (cp[2] * tt[2] / eta[13]) * (Utilities.fpow(prat[13], (gam[2] - 1.0) / gam[2]) - 1.0);
				deltf = delhf / cp[2];
				tt[13] = tt[2] + deltf;
				pt[13] = pt[2] * prat[13];
				trat[13] = tt[13] / tt[2];
				gam[13] = Utilities.getGama(tt[13]);
				cp[13] = Utilities.getCp(tt[13]);
				prat[3] = p3p2d; // core compressor
				if (prat[3] < .5)
					prat[3] = .5;

				delhc = (cp[13] * tt[13] / eta[3]) * (Utilities.fpow(prat[3], (gam[13] - 1.0) / gam[13]) - 1.0);
				deltc = delhc / cp[13];
				tt[3] = tt[13] + deltc;
				pt[3] = pt[13] * prat[3];
				trat[3] = tt[3] / tt[13];
				gam[3] = Utilities.getGama(tt[3]);
				cp[3] = Utilities.getCp(tt[3]);
				tt[4] = tt4 * throtl / 100.0;
				pt[4] = pt[3] * prat[4];
				gam[4] = Utilities.getGama(tt[4]);
				cp[4] = Utilities.getCp(tt[4]);
				trat[4] = tt[4] / tt[3];
				delhht = delhc;
				deltht = delhht / cp[4];
				tt[5] = tt[4] - deltht;
				gam[5] = Utilities.getGama(tt[5]);
				cp[5] = Utilities.getCp(tt[5]);
				trat[5] = tt[5] / tt[4];
				prat[5] = Utilities.fpow((1.0 - delhht / cp[4] / tt[4] / eta[5]), (gam[4] / (gam[4] - 1.0)));
				pt[5] = pt[4] * prat[5];
				delhlt = (1.0 + byprat) * delhf;
				deltlt = delhlt / cp[5];
				tt[15] = tt[5] - deltlt;
				gam[15] = Utilities.getGama(tt[15]);
				cp[15] = Utilities.getCp(tt[15]);
				trat[15] = tt[15] / tt[5];
				prat[15] = Utilities.fpow((1.0 - delhlt / cp[5] / tt[5] / eta[5]), (gam[5] / (gam[5] - 1.0)));
				pt[15] = pt[5] * prat[15];
			}

			tt[7] = tt7;

			prat[6] = 1.0;
			pt[6] = pt[15];
			trat[6] = 1.0;
			tt[6] = tt[15];
			gam[6] = Utilities.getGama(tt[6]);
			cp[6] = Utilities.getCp(tt[6]);

			tt[7] = tt[6] * trat[7];
			pt[7] = pt[6] * prat[7];
			gam[7] = Utilities.getGama(tt[7]);
			cp[7] = Utilities.getCp(tt[7]);

			// engine press ratio EPReair
			epr = prat[7] * prat[15] * prat[5] * prat[4] * prat[3] * prat[13];
			// engine temp ratio ETR
			etr = trat[7] * trat[15] * trat[5] * trat[4] * trat[3] * trat[13];
		}

		// Utility to determine engine performance
		public void calcPerf() {
			double fac1, game, cpe, cp3;

			cp3 = Utilities.getCp(tt[3]); // BTU/lbm R
			ues = 0.0;
			game = Utilities.getGama(tt[5]);
			fac1 = (game - 1.0) / game;
			cpe = Utilities.getCp(tt[5]);
			if (eta[7] < .8)
				eta[7] = .8; // protection during overwriting
			if (eta[4] < .8)
				eta[4] = .8;
			pt[8] = epr * prat[2] * pt[0];
			eair = Utilities.getAir(1.0, game) * 144.0 * a8 * pt[8] / 14.7 / Utilities.sqroot(etr * tt[0] / 518.0);
			m2 = Utilities
				.getMach(0, eair * Utilities.sqroot(tt[0] / 518.0) / (prat[2] * pt[0] / 14.7 * acore * 144.0), gama);
			npr = pt[8] / ps0;
			uexit = Utilities.sqroot(2.0 * Rgas / fac1 * etr * tt[0] * eta[7] * (1.0 - Utilities.fpow(1.0 / npr, fac1)));

			if (npr <= 1.893)
				pexit = ps0;
			else
				pexit = .52828 * pt[8];

			fgros = (uexit + (pexit - ps0) * 144.0 * a8 / eair) / g0;

			// turbo fan -- added terms for fan flow
			if (engine == 3) {
				fac1 = (gama - 1.0) / gama;
				snpr = pt[13] / ps0;
				ues = Utilities.sqroot(2.0 * Rgas / fac1 * tt[13] * eta[7] * (1.0 - Utilities.fpow(1.0 / snpr, fac1)));
				m2 = Utilities.getMach(0, eair * (1.0 + byprat) * Utilities.sqroot(tt[0] / 518.0)
				                       / (prat[2] * pt[0] / 14.7 * afan * 144.0), gama);

				if (snpr <= 1.893)
					pfexit = ps0;
				else
					pfexit = .52828 * pt[13];
				fgros = fgros + (byprat * ues + (pfexit - ps0) * 144.0 * byprat * acore / eair) / g0;
			}

			// ram drag
			dram = u0 / g0;
			if (engine == 3)
				dram = dram + u0 * byprat / g0;

			// mass flow ratio
			if (fsmach > .01)
				mfr = Utilities.getAir(m2, gama) * prat[2] / Utilities.getAir(fsmach, gama);
			else
				mfr = 5.0;

			// net thrust
			fnet = fgros - dram;

			// thrusts in pounds
			fnlb = fnet * eair;
			fglb = fgros * eair;
			drlb = dram * eair;

			// fuel-air ratio and sfc
			fa = (trat[4] - 1.0) / (eta[4] * fhv / (cp3 * tt[3]) - trat[4]) + (trat[7] - 1.0)
				/ (fhv / (cpe * tt[15]) - trat[7]);
			if (fnet > 0.0) {
				sfc = 3600.0 * fa / fnet;
				if (sfc < 0)
					sfc = 0.0;
				flflo = sfc * fnlb;
				isp = (fnlb / flflo) * 3600.0;
			} else {
				fnlb = 0.0;
				flflo = 0.0;
				sfc = 0.0;
				isp = 0.0;
			}

			// weight calculation
			if (engine == 1) {
				weight = .12754 * Utilities.sqroot(acore * acore * acore)
					* (dcomp * lcomp + dburner * lburn + dturbin * lturb + dnozl * lnoz);
			}
			if (engine == 2) {
				weight = .08533 * Utilities.sqroot(acore * acore * acore)
					* (dcomp * lcomp + dburner * lburn + dturbin * lturb + dnozl * lnoz);
			}
			if (engine == 3) {
				weight = .08955 * acore
					* ((1.0 + byprat) * dfan * 4.0 + dcomp * (ncomp - 3) + dburner + dturbin * nturb + dburner * 2.0)
						* Utilities.sqroot(acore / 6.965);
			}
		}

		// Utility to determine geometric variables
		public void getGeo() {
			// limits compressor face
			a8max = .75 * Utilities.sqroot(etr) / epr;

			// mach number to < .5
			if (a8max > 1.0)
				a8max = 1.0;

			if (a8rat > a8max)
				a8rat = a8max;

			// dumb down limit - a8 schedule
			// if (arsched == 0)
			a8rat = a8max;

			a8 = a8rat * acore;
			a8d = a8 * prat[7] / Utilities.sqroot(trat[7]);

			// assumes choked a8 and a4
			a4 = a8 * prat[5] * prat[15] * prat[7] / Utilities.sqroot(trat[7] * trat[5] * trat[15]);
			a4p = a8 * prat[15] * prat[7] / Utilities.sqroot(trat[7] * trat[15]);

			// size parameters for weight
			ncomp = (int) (1.0 + p3p2d / 1.5);
			if (ncomp > 15)
				ncomp = 15;

			sblade = .02;
			hblade = Utilities.sqroot(2.0 / 3.1415926);
			tblade = .2 * hblade;

			xcomp = ncomp * (tblade + sblade);
			ncompd = ncomp;
			if (engine == 3) { // fan geometry
				ncompd = ncomp + 3;
				xcomp = ncompd * (tblade + sblade);
			}

			lcomp = xcomp;
			lburn = hblade;

			nturb = 1 + ncomp / 4;
			if (engine == 3)
				nturb = nturb + 1;

			lturb = nturb * (tblade + sblade);
			lnoz = lburn;
			if (engine == 2)
				lnoz = 3.0 * lburn;

		}
	}
}

