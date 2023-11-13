using System;

namespace CSharpBenchmark
{
	public class Utilities
	{
		public const double PRECISION = configuration_data.PRECISION;

		/* Utility to convert degree in radian */
		public static double deg2rad(double deg, double pi) {
			return (deg / 180 * pi);
		}

		/* Utility to get gamma as a function of temperature */
		public static double getGama(double temp) {
			double number, a, b, c, d;
			a = -7.6942651e-13;
			b = 1.3764661e-08;
			c = -7.8185709e-05;
			d = 1.436914;
			number = a * temp * temp * temp + b * temp * temp + c * temp + d;
			return (number);
		}

		/* Utility to get the Mach number given the corrected airflow per area */
		public static double getMach(int sub, double corair, double gama1) {
			double number, chokair; // iterate for mach number
			double deriv, machn, macho, airo, airn;
			int iter;
			chokair = getAir(1.0, gama1);
			if (corair > chokair) {
				number = 1.0;
				return (number);
			} else {
				airo = .25618; // initial guess
				if (sub == 1)
					macho = 1.0; // sonic
				else {
					if (sub == 2)
						macho = 1.703; // supersonic
					else
						macho = .5; // subsonic
					iter = 1;
					machn = macho - .2;
					while (Math.Abs(corair - airo) > .0001 && iter < 20) {
						airn = getAir(machn, gama1);
						deriv = (airn - airo) / (machn - macho);
						airo = airn;
						macho = machn;
						machn = macho + (corair - airo) / deriv;
						++iter;
					}
				}
				number = macho;
			}
			return (number);
		}

		/* Utility to get the corrected airflow per area given the Mach number */
		public static double getAir(double mach, double gama2) {
			double number, fac1, fac2;
			fac2 = (gama2 + 1.0) / (2.0 * (gama2 - 1.0));
			fac1 = fpow((1.0 + .5 * (gama2 - 1.0) * mach * mach), fac2);
			number = .50161 * sqroot(gama2) * mach / fac1;

			return (number);
		}

		/* Analysis for Rayleigh flow */
		public static double getRayleighLoss(double mach1, double ttrat, double tlow) {
			double number;
			double wc1, wc2, mgueso, mach2, g1, gm1, g2, gm2;
			double fac1, fac2, fac3, fac4;
			g1 = getGama(tlow);
			gm1 = g1 - 1.0;
			wc1 = getAir(mach1, g1);
			g2 = getGama(tlow * ttrat);
			gm2 = g2 - 1.0;
			number = .95;
			// iterate for mach downstream
			mgueso = .4; // initial guess
			mach2 = .5;
			while (Math.Abs(mach2 - mgueso) > .0001) {
				mgueso = mach2;
				fac1 = 1.0 + g1 * mach1 * mach1;
				fac2 = 1.0 + g2 * mach2 * mach2;
				fac3 = fpow((1.0 + .5 * gm1 * mach1 * mach1), (g1 / gm1));
				fac4 = fpow((1.0 + .5 * gm2 * mach2 * mach2), (g2 / gm2));
				number = fac1 * fac4 / fac2 / fac3;
				wc2 = wc1 * sqroot(ttrat) / number;
				mach2 = getMach(0, wc2, g2);
			}
			return (number);
		}

		/* Utility to get cp as a function of temperature */
		public static double getCp(double temp) {
			double number, a, b, c, d;
			// BTU/R
			a = -4.4702130e-13;
			b = -5.1286514e-10;
			c = 2.8323331e-05;
			d = 0.2245283;
			number = a * temp * temp * temp + b * temp * temp + c * temp + d;
			return (number);
		}

		// *********** Math utilities ***********
		public static double fpow(double x, double y) {
			int partieEntiere = (int) y;

			// If x<0 and y not integer
			if (x < 0 && (double) partieEntiere != y) {
				Console.Write("error power undefined\n");
				return 0;
			}

			// If x<0 and y integer
			else if (x < 0)
				return power(x, partieEntiere);

			// now x>0
			// factorize y into integer and decimal parts
			// For example : 12.345^67.890123 = (12.345^67) * (12.345^0.890123)
			// integer part : pow(double, int) and the decimal par : x^y =
			// exp(y*ln(x))
			return power(x, partieEntiere) * expo((y - partieEntiere) * log(x));
		}

		public static double sqroot(double number) {
			double x0, x, prec = 1;
			if (number < 0) {
				Console.Write("error sqroot\n");
				return (0);
			}

			x = (1 + number) / 2;
			while ((prec > 0.0001) || (prec < -0.0001)) {
				x0 = x;
				x = 0.5 * (x0 + number / x0);
				prec = (x - x0) / x0;
			}
			return (x);
		}

		public static double fabs(double x) {
			if (x < 0)
				return -x;
			else
				return x;
		}

		public static double log(double x) {
			double number = 0;
			double coeff = -1;
			int i = 1;
			if (x <= 0) {
				Console.Write("error log undefined\n");
				return 0;
			}

			if (x == 1)
				return 0;

			if (x > 1)
				return -log(1 / x);

			// 0<x<1
			// log : x - x^2/2 + x^3/3 - x^4/4...
			while (fabs(coeff) > PRECISION) {
				coeff *= 1 - x;
				number += coeff / i;
				i++;
			}
			return number;
		}

		public static double power(double x, int y) {
			double number = 1;
			int i;
			// x^(-y) = 1/(x^y)
			if (y < 0)
				return 1 / (power(x, -y));

			for (i = 0; i < y; i++)
				number *= x;

			return number;
		}

		public static double expo(double x) {
			double number = 1;
			double coeff = 1;
			int i = 1;
			// if x > log(DBL_MAX)
			if (x > 709.782712893384)
				return expo(709.78); // Infinite value

			// exp : x^0/0! + x^1/1! + x^2/2! + x^3/3!
			while (fabs(coeff) > PRECISION) {
				coeff *= x / i;
				number += coeff;
				i++;
			}

			return number;
		}
	}
}

