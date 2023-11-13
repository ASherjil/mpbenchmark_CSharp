using System;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace CSharpBenchmark
{
	class mpbenchmark
	{
		//number of threads
		public static int NUM_THREADS=configuration_data.NUM_THREADS;
		private static Thread[] threads=new Thread[NUM_THREADS];
		private static Stopwatch watch = new Stopwatch();

		//arrays, store input and output data
		public static double[,]? inputArray;
		public static double[,]? outputArray;	
		public static int LineCount=0;

		public static void Main (string[] args)
		{
			//Console.WriteLine ("Multi Processor Benchmark C# version\n");
			//Console.WriteLine("Number Of Processors: {0}", NUM_THREADS);
			int engine=3;
			if(args.Length>0)
			{
				if(string.Equals(args[0],"1")){
					engine=1;
					//Console.WriteLine("Turbojet is selected");
				}
				else if(string.Equals(args[0],"2")){
					engine=2;
					//Console.WriteLine("Afterburner  is selected");
				}
				else if(string.Equals(args[0],"3")){
					engine=3;
					//Console.WriteLine("Turbofan  is selected");
				}
				else{
					//Console.WriteLine("Turbofan (default) is selected");
				}
			}
			else
			{
				Console.WriteLine("Turbofan (default) is selected");
			}
			configuration_data.engine = engine;

			InitializeArray ();

			watch.Start ();

			for (int i=0; i<NUM_THREADS; i++) {
				Worker workerObject = new Worker(i);
				threads[i] = new Thread(workerObject.run);
				threads[i].Start();
			}
			for (int i=0; i<NUM_THREADS; i++) {
				threads [i].Join ();
			}

			watch.Stop ();
			printResult ();
		}


		public static void InitializeArray(){

			StreamReader sr = File.OpenText ("..//IOFiles//InputFile.txt");
			LineCount=0;
			string? sCurrentLine;
			while ((sCurrentLine = sr.ReadLine()) != null)
			{
				LineCount++;
			}
			inputArray=new double[LineCount,4];
			outputArray=new double[LineCount,18];
			sr.Close ();
			sr = File.OpenText ("..//IOFiles//InputFile.txt");
			sCurrentLine = null;
			LineCount=0;
			while( (sCurrentLine = sr.ReadLine()) != null){
				String[] dataFromFile=sCurrentLine.Split(new char[1]{' '});
				inputArray[LineCount,0]=Double.Parse(dataFromFile[0]);
				inputArray[LineCount,1]=Double.Parse(dataFromFile[1]);
				inputArray[LineCount,2]=Double.Parse(dataFromFile[2]);
				inputArray[LineCount,3]=Double.Parse(dataFromFile[3]);
				LineCount++;
			}
		}

		public static void printResult(){
			//header for results
			/*
			Console.Write("T,ExecTime,  Spd| Alt |  Thr| Mach|Press| Temp| Fnet|Fgros|RamDr|FlFlo|TSFC|Airfl|Weight|Fn/W\n");
			for(int i=0;i<LineCount;i++){
				Console.Write(String.Format("{0:d},{1:f7}, {2:f0}|{3:f0}|{4:f1}|{5:f3}|{6:2}|{7:f1}|{8:f0}|{9:f0}|{10:f0}|{11:f0}|{12:f2}|{13:f1}|{14:f2}|{15:f2}\n     {16:f1}% used for point {17:d}\n"
				                            , (int)outputArray[i,0],outputArray[i,1],outputArray[i,2],outputArray[i,3],outputArray[i,4],
				                            outputArray[i,5],outputArray[i,6],outputArray[i,7],outputArray[i,8],outputArray[i,9],outputArray[i,10],
				                            outputArray[i,11],outputArray[i,12],outputArray[i,13],outputArray[i,14],outputArray[i,15], 
				                            outputArray[i,16], (int)outputArray[i,17]));
			}
			*/
			Console.Write(String.Format("{0:d}\n", rtPerformancedata.getNumMissed()));		
			//Console.Write(String.Format("Thread response time sum:{0:f6}\n",rtPerformancedata.getTotalTime()));
			Console.Write(String.Format("Number of threads : {0:d}\n", NUM_THREADS));
			Console.Write(String.Format("Number of points : {0:d}\n",rtPerformancedata.getNumPoints()));
			double benchmarkTotalTime = watch.Elapsed.TotalSeconds;
			Console.Write(String.Format("{0:f6}\n", benchmarkTotalTime));
			//write into file>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>
			using (StreamWriter sw = new StreamWriter("..//IOFiles//CSharp//ResponseTimeForCore"+NUM_THREADS+".txt", true, Encoding.Default))
			{
				sw.WriteLine(benchmarkTotalTime);
				sw.Close();
			}
			using (StreamWriter sw = new StreamWriter("..//IOFiles//CSharp//DeadLineForCore"+NUM_THREADS+".txt", true, Encoding.Default))
			{
				sw.WriteLine(rtPerformancedata.getNumMissed());
				sw.Close();
			}

		}
	}
}
