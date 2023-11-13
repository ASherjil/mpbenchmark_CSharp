using System;

namespace CSharpBenchmark
{
	public static class rtPerformancedata
	{
		private static Object mylock= new Object ();
		private static int NumPoints = 0, NumMissed = 0;
		private static double TotalTimePoint = 0;
		private static double TotalTime = 0, TotalUsed = 0;

		public static int nextNumPoints() {
			lock(mylock){
				return ++NumPoints;
			}
		}
		public static void undonextNumPoints() {
			lock(mylock){
				--NumPoints;
			}
		}
		public static int getNumPoints() {
			lock (mylock) {
				return NumPoints;
			}
		}

		public static void increaseNumMissed(int byValue) {
			lock (mylock) {
				NumMissed += byValue;
			}
		}
		public static int getNumMissed() {
			lock (mylock) {
				return NumMissed;
			}
		}

		public static void increaseTotalTimePoint(double byValue) {
			lock (mylock) {
				TotalTimePoint += byValue;
			}
		}
		public static double getTotalTimePoint() {
			lock (mylock) {
				return TotalTimePoint;
			}
		}

		public static void increaseTotalTime(double byValue) {
			lock (mylock) {
				TotalTime += byValue;
			}
		}
		public static double getTotalTime() {
			lock (mylock) {
				return TotalTime;
			}
		}

		public static void increaseTotalUsed(double byValue) {
			lock (mylock) {
				TotalUsed += byValue;
			}
		}
		public static double getTotalUsed() {
			lock (mylock) {
				return TotalUsed;
			}
		}

	}
}

