﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ionic.Zlib.Tests
{
	/// <summary>
	/// Summary description for UnitTest1
	/// </summary>
	[TestClass]
	public class UnitTest1
	{
        Random rnd;
		Dictionary<string, string> TestStrings;

		public UnitTest1()
		{
			rnd = new Random();
			TestStrings = new Dictionary<string, string>()
				{
					{ "LetMeDoItNow", LetMeDoItNow },
					{ "GoPlacidly", GoPlacidly },
					{ "IhaveaDream", IhaveaDream },
					{ "LoremIpsum", LoremIpsum },
				};
		}

		static UnitTest1() =>
			LoremIpsumWords = LoremIpsum.Split(" ".ToCharArray(),
				StringSplitOptions.RemoveEmptyEntries);


		private TestContext testContextInstance;

		/// <summary>
		///Gets or sets the test context which provides
		///information about and functionality for the current test run.
		///</summary>
		public TestContext TestContext
        {
            get => testContextInstance;
            set => testContextInstance = value;
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //

        private string CurrentDir = null;
		private string TopLevelDir = null;

		// Use TestInitialize to run code before running each test
		[TestInitialize()]
		public void MyTestInitialize()
		{
			CurrentDir = Directory.GetCurrentDirectory();
			Assert.AreNotEqual(Path.GetFileName(CurrentDir), "Temp", "at start");

			var parentDir = Environment.GetEnvironmentVariable("TEMP");

			TopLevelDir = Path.Combine(parentDir, $"Ionic.ZlibTest-{DateTime.Now.ToString("yyyyMMMdd-HHmmss")}.tmp");
			Directory.CreateDirectory(TopLevelDir);
			Directory.SetCurrentDirectory(TopLevelDir);
		}


		// Use TestCleanup to run code after each test has run
		[TestCleanup()]
		public void MyTestCleanup()
		{
			Directory.SetCurrentDirectory(Environment.GetEnvironmentVariable("TEMP"));
			Directory.Delete(TopLevelDir, true);
			Assert.AreNotEqual(Path.GetFileName(CurrentDir), "Temp", "at finish");
			Directory.SetCurrentDirectory(CurrentDir);
		}


		#endregion

		#region Helpers
		/// <summary>
		/// Converts a string to a MemoryStream.
		/// </summary>
		static MemoryStream StringToMemoryStream(string s)
		{
            var enc = new ASCIIEncoding();
			var byteCount = enc.GetByteCount(s.ToCharArray(), 0, s.Length);
			var ByteArray = new byte[byteCount];
			var bytesEncodedCount = enc.GetBytes(s, 0, s.Length, ByteArray, 0);
            var ms = new MemoryStream(ByteArray);
			return ms;
		}

		/// <summary>
		/// Converts a MemoryStream to a string. Makes some assumptions about the content of the stream.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		static string MemoryStreamToString(MemoryStream ms)
		{
			var ByteArray = ms.ToArray();
            var enc = new ASCIIEncoding();
			var s = enc.GetString(ByteArray);
			return s;
		}

		private static void CopyStream(Stream src, Stream dest)
		{
			var buffer = new byte[1024];
			var len = 0;
			while ((len = src.Read(buffer, 0, buffer.Length)) > 0)
			{
				dest.Write(buffer, 0, len);
			}
			dest.Flush();
		}

		private static string GetTestDependentDir(string startingPoint, string subdir)
		{
			var location = startingPoint;
			for (var i = 0; i < 3; i++)
				location = Path.GetDirectoryName(location);

			location = Path.Combine(location, subdir);
			return location;
		}

		private static string GetTestBinDir(string startingPoint) => GetTestDependentDir(startingPoint, "Zlib Tests\\bin\\Debug");

		private string GetContentFile(string fileName)
		{
			var testBin = GetTestBinDir(CurrentDir);
			var path = Path.Combine(testBin, $"Resources\\{fileName}");
			Assert.IsTrue(File.Exists(path), "file ({0}) does not exist", path);
			return path;
		}

		internal string Exec(string program, string args) => Exec(program, args, true);

		internal string Exec(string program, string args, bool waitForExit) => Exec(program, args, waitForExit, true);

		internal string Exec(string program, string args, bool waitForExit, bool emitOutput)
		{
			if (program == null)
				throw new ArgumentException("program");

			if (args == null)
				throw new ArgumentException("args");

			// Microsoft.VisualStudio.TestTools.UnitTesting
			TestContext.WriteLine("running command: {0} {1}", program, args);

			string output;
			var rc = Exec_NoContext(program, args, waitForExit, out output);

			if (rc != 0)
				throw new Exception($"Non-zero RC {program}: {output}");

			if (emitOutput)
				TestContext.WriteLine("output: {0}", output);
			else
				TestContext.WriteLine("A-OK. (output suppressed)");

			return output;
		}

		internal static int Exec_NoContext(string program, string args, bool waitForExit, out string output)
		{
			var p = new System.Diagnostics.Process
			{
				StartInfo =
				{
					FileName = program,
					CreateNoWindow = true,
					Arguments = args,
					WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
					UseShellExecute = false,
				}
			};

			if (waitForExit)
			{
				var sb = new StringBuilder();
				p.StartInfo.RedirectStandardOutput = true;
				p.StartInfo.RedirectStandardError = true;
				// must read at least one of the stderr or stdout asynchronously,
				// to avoid deadlock
				Action<object, System.Diagnostics.DataReceivedEventArgs> stdErrorRead = (o, e) =>
				{
					if (!string.IsNullOrEmpty(e.Data))
						sb.Append(e.Data);
				};

				p.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(stdErrorRead);
				p.Start();
				p.BeginErrorReadLine();
				output = p.StandardOutput.ReadToEnd();
				p.WaitForExit();
				if (sb.Length > 0)
					output += sb.ToString();
				//output = CleanWzzipOut(output); // just in case
				return p.ExitCode;
			}
			else
			{
				p.Start();
			}
			output = "";
			return 0;
		}

		#endregion

		//TODO: Find out why we can't load this file
		[TestMethod]
		public void zlib_Compat_decompress_wi13446()
		{
			var zlibbedFile = Path.Combine(AppContext.BaseDirectory, "zlibbed.file");
			var streamCopy = new Action<Stream, Stream, int>((source, dest, bufferSize) =>
			{
				var temp = new byte[bufferSize];
				while (true)
				{
					var read = source.Read(temp, 0, temp.Length);
					if (read <= 0) break;
					dest.Write(temp, 0, read);
				}
			});

			var unpack = new Action<int>((bufferSize) =>
			{
				using var output = new MemoryStream();
				using var input = File.OpenRead(zlibbedFile);
				using var zinput = new ZlibStream(input, CompressionMode.Decompress);
				streamCopy(zinput, output, bufferSize);
			});

			unpack(1024);
			unpack(16384);
		}


		[TestMethod]
		public void Zlib_BasicDeflateAndInflate()
		{
			var TextToCompress = LoremIpsum;

			int rc;
			var bufferSize = 40000;
			var compressedBytes = new byte[bufferSize];
			var decompressedBytes = new byte[bufferSize];

			var compressingStream = new ZlibCodec();

			rc = compressingStream.InitializeDeflate(CompressionLevel.Default);
			Assert.AreEqual(ZlibConstants.Z_OK, rc, $"at InitializeDeflate() [{compressingStream.Message}]");

			compressingStream.InputBuffer = Encoding.ASCII.GetBytes(TextToCompress);
			compressingStream.NextIn = 0;

			compressingStream.OutputBuffer = compressedBytes;
			compressingStream.NextOut = 0;

			while (compressingStream.TotalBytesIn != TextToCompress.Length && compressingStream.TotalBytesOut < bufferSize)
			{
				compressingStream.AvailableBytesIn = compressingStream.AvailableBytesOut = 1; // force small buffers
				rc = compressingStream.Deflate(FlushType.None);
				Assert.AreEqual(ZlibConstants.Z_OK, rc, $"at Deflate(1) [{compressingStream.Message}]");
			}

			while (true)
			{
				compressingStream.AvailableBytesOut = 1;
				rc = compressingStream.Deflate(FlushType.Finish);
				if (rc == ZlibConstants.Z_STREAM_END)
					break;
				Assert.AreEqual(ZlibConstants.Z_OK, rc, $"at Deflate(2) [{compressingStream.Message}]");
			}

			rc = compressingStream.EndDeflate();
			Assert.AreEqual(ZlibConstants.Z_OK, rc, $"at EndDeflate() [{compressingStream.Message}]");

			var decompressingStream = new ZlibCodec();

			decompressingStream.InputBuffer = compressedBytes;
			decompressingStream.NextIn = 0;
			decompressingStream.OutputBuffer = decompressedBytes;
			decompressingStream.NextOut = 0;

			rc = decompressingStream.InitializeInflate();
			Assert.AreEqual(ZlibConstants.Z_OK, rc, $"at InitializeInflate() [{decompressingStream.Message}]");
			//CheckForError(decompressingStream, rc, "inflateInit");

			while (decompressingStream.TotalBytesOut < decompressedBytes.Length && decompressingStream.TotalBytesIn < bufferSize)
			{
				decompressingStream.AvailableBytesIn = decompressingStream.AvailableBytesOut = 1; /* force small buffers */
				rc = decompressingStream.Inflate(FlushType.None);
				if (rc == ZlibConstants.Z_STREAM_END)
					break;
				Assert.AreEqual(ZlibConstants.Z_OK, rc, $"at Inflate() [{decompressingStream.Message}]");
				//CheckForError(decompressingStream, rc, "inflate");
			}

			rc = decompressingStream.EndInflate();
			Assert.AreEqual(ZlibConstants.Z_OK, rc, $"at EndInflate() [{decompressingStream.Message}]");
			//CheckForError(decompressingStream, rc, "inflateEnd");

			var j = 0;
			for (; j < decompressedBytes.Length; j++)
				if (decompressedBytes[j] == 0)
					break;

			Assert.AreEqual(TextToCompress.Length, j, string.Format("Unequal lengths"));

			var i = 0;
			for (i = 0; i < j; i++)
				if (TextToCompress[i] != decompressedBytes[i])
					break;

			Assert.AreEqual(j, i, string.Format("Non-identical content"));

			var result = Encoding.ASCII.GetString(decompressedBytes, 0, j);

			TestContext.WriteLine("orig length: {0}", TextToCompress.Length);
			TestContext.WriteLine("compressed length: {0}", compressingStream.TotalBytesOut);
			TestContext.WriteLine("decompressed length: {0}", decompressingStream.TotalBytesOut);
			TestContext.WriteLine("result length: {0}", result.Length);
			TestContext.WriteLine("result of inflate:\n{0}", result);
			return;
		}





		[TestMethod]
		public void Zlib_BasicDictionaryDeflateInflate()
		{
			int rc;
			var comprLen = 40000;
			var uncomprLen = comprLen;
			var uncompr = new byte[uncomprLen];
			var compr = new byte[comprLen];
			//long dictId;

			var compressor = new ZlibCodec();
			rc = compressor.InitializeDeflate(CompressionLevel.BestCompression);
			Assert.AreEqual(ZlibConstants.Z_OK, rc, $"at InitializeDeflate() [{compressor.Message}]");

			var dictionaryWord = "hello ";
			var dictionary = Encoding.ASCII.GetBytes(dictionaryWord);
			var TextToCompress = "hello, hello!  How are you, Joe? I said hello. ";
			var BytesToCompress = Encoding.ASCII.GetBytes(TextToCompress);

			rc = compressor.SetDictionary(dictionary);
			Assert.AreEqual(ZlibConstants.Z_OK, rc, $"at SetDeflateDictionary() [{compressor.Message}]");

			var dictId = compressor.Adler32;

			compressor.OutputBuffer = compr;
			compressor.NextOut = 0;
			compressor.AvailableBytesOut = comprLen;

			compressor.InputBuffer = BytesToCompress;
			compressor.NextIn = 0;
			compressor.AvailableBytesIn = BytesToCompress.Length;

			rc = compressor.Deflate(FlushType.Finish);
			Assert.AreEqual(ZlibConstants.Z_STREAM_END, rc, $"at Deflate() [{compressor.Message}]");

			rc = compressor.EndDeflate();
			Assert.AreEqual(ZlibConstants.Z_OK, rc, $"at EndDeflate() [{compressor.Message}]");


			var decompressor = new ZlibCodec();

			decompressor.InputBuffer = compr;
			decompressor.NextIn = 0;
			decompressor.AvailableBytesIn = comprLen;

			rc = decompressor.InitializeInflate();
			Assert.AreEqual(ZlibConstants.Z_OK, rc, $"at InitializeInflate() [{decompressor.Message}]");

			decompressor.OutputBuffer = uncompr;
			decompressor.NextOut = 0;
			decompressor.AvailableBytesOut = uncomprLen;

			while (true)
			{
				rc = decompressor.Inflate(FlushType.None);
				if (rc == ZlibConstants.Z_STREAM_END)
				{
					break;
				}
				if (rc == ZlibConstants.Z_NEED_DICT)
				{
					Assert.AreEqual<long>(dictId, decompressor.Adler32, "Unexpected Dictionary");
					rc = decompressor.SetDictionary(dictionary);
				}
				Assert.AreEqual(ZlibConstants.Z_OK, rc, $"at Inflate/SetInflateDictionary() [{decompressor.Message}]");
			}

			rc = decompressor.EndInflate();
			Assert.AreEqual(ZlibConstants.Z_OK, rc, $"at EndInflate() [{decompressor.Message}]");

			var j = 0;
			for (; j < uncompr.Length; j++)
				if (uncompr[j] == 0)
					break;

			Assert.AreEqual(TextToCompress.Length, j, string.Format("Unequal lengths"));

			var i = 0;
			for (i = 0; i < j; i++)
				if (TextToCompress[i] != uncompr[i])
					break;

			Assert.AreEqual(j, i, string.Format("Non-identical content"));

			var result = Encoding.ASCII.GetString(uncompr, 0, j);

			TestContext.WriteLine("orig length: {0}", TextToCompress.Length);
			TestContext.WriteLine("compressed length: {0}", compressor.TotalBytesOut);
			TestContext.WriteLine("uncompressed length: {0}", decompressor.TotalBytesOut);
			TestContext.WriteLine("result length: {0}", result.Length);
			TestContext.WriteLine("result of inflate:\n{0}", result);
		}

		[TestMethod]
		public void Zlib_TestFlushSync()
		{
			int rc;
			var bufferSize = 40000;
			var CompressedBytes = new byte[bufferSize];
			var DecompressedBytes = new byte[bufferSize];
			var TextToCompress = "This is the text that will be compressed.";
			var BytesToCompress = Encoding.ASCII.GetBytes(TextToCompress);

			var compressor = new ZlibCodec(CompressionMode.Compress);

			compressor.InputBuffer = BytesToCompress;
			compressor.NextIn = 0;
			compressor.AvailableBytesIn = 3;

			compressor.OutputBuffer = CompressedBytes;
			compressor.NextOut = 0;
			compressor.AvailableBytesOut = CompressedBytes.Length;

			rc = compressor.Deflate(FlushType.Full);

			CompressedBytes[3]++; // force an error in first compressed block // dinoch - ??
			compressor.AvailableBytesIn = TextToCompress.Length - 3;

			rc = compressor.Deflate(FlushType.Finish);
			Assert.AreEqual(ZlibConstants.Z_STREAM_END, rc, $"at Deflate() [{compressor.Message}]");

			rc = compressor.EndDeflate();
			bufferSize = (int)compressor.TotalBytesOut;

			var decompressor = new ZlibCodec(CompressionMode.Decompress);

			decompressor.InputBuffer = CompressedBytes;
			decompressor.NextIn = 0;
			decompressor.AvailableBytesIn = 2;

			decompressor.OutputBuffer = DecompressedBytes;
			decompressor.NextOut = 0;
			decompressor.AvailableBytesOut = DecompressedBytes.Length;

			rc = decompressor.Inflate(FlushType.None);
			decompressor.AvailableBytesIn = bufferSize - 2;

			rc = decompressor.SyncInflate();

			var gotException = false;
			try
			{
				rc = decompressor.Inflate(FlushType.Finish);
			}
			catch (ZlibException ex1)
			{
				TestContext.WriteLine("Got Expected Exception: " + ex1);
				gotException = true;
			}

			Assert.IsTrue(gotException, "inflate should report DATA_ERROR");

			rc = decompressor.EndInflate();
			Assert.AreEqual(ZlibConstants.Z_OK, rc, $"at EndInflate() [{decompressor.Message}]");

			var j = 0;
			for (; j < DecompressedBytes.Length; j++)
				if (DecompressedBytes[j] == 0)
					break;

			var result = Encoding.ASCII.GetString(DecompressedBytes, 0, j);

			Assert.AreEqual(TextToCompress.Length, result.Length + 3, "Strings are unequal lengths");

			Console.WriteLine("orig length: {0}", TextToCompress.Length);
			Console.WriteLine("compressed length: {0}", compressor.TotalBytesOut);
			Console.WriteLine("uncompressed length: {0}", decompressor.TotalBytesOut);
			Console.WriteLine("result length: {0}", result.Length);
			Console.WriteLine("result of inflate:\n(Thi){0}", result);
		}

		[TestMethod]
		public void Zlib_Codec_TestLargeDeflateInflate()
		{
			int rc;
			int j;
			var bufferSize = 80000;
			var compressedBytes = new byte[bufferSize];
			var workBuffer = new byte[bufferSize / 4];

			var compressingStream = new ZlibCodec();

			rc = compressingStream.InitializeDeflate(CompressionLevel.Level1);
			Assert.AreEqual(ZlibConstants.Z_OK, rc, $"at InitializeDeflate() [{compressingStream.Message}]");

			compressingStream.OutputBuffer = compressedBytes;
			compressingStream.AvailableBytesOut = compressedBytes.Length;
			compressingStream.NextOut = 0;
            var rnd = new Random();

			for (var k = 0; k < 4; k++)
			{
				switch (k)
				{
					case 0:
						// At this point, workBuffer is all zeroes, so it should compress very well.
						break;

					case 1:
						// switch to no compression, keep same workBuffer (all zeroes):
						compressingStream.SetDeflateParams(CompressionLevel.None, CompressionStrategy.Default);
						break;

					case 2:
						// Insert data into workBuffer, and switch back to compressing mode.
						// we'll use lengths of the same random byte:
						for (var i = 0; i < workBuffer.Length / 1000; i++)
						{
							var b = (byte)rnd.Next();
							var n = 500 + rnd.Next(500);
							for (j = 0; j < n; j++)
								workBuffer[j + i] = b;
							i += j - 1;
						}
						compressingStream.SetDeflateParams(CompressionLevel.BestCompression, CompressionStrategy.Filtered);
						break;

					case 3:
						// insert totally random data into the workBuffer
						rnd.NextBytes(workBuffer);
						break;
				}

				compressingStream.InputBuffer = workBuffer;
				compressingStream.NextIn = 0;
				compressingStream.AvailableBytesIn = workBuffer.Length;
				rc = compressingStream.Deflate(FlushType.None);
				Assert.AreEqual(ZlibConstants.Z_OK, rc, $"at Deflate({k}) [{compressingStream.Message}]");

				if (k == 0)
					Assert.AreEqual(0, compressingStream.AvailableBytesIn, "Deflate should be greedy.");

				TestContext.WriteLine("Stage {0}: uncompressed/compresssed bytes so far:  ({1,6}/{2,6})",
					  k, compressingStream.TotalBytesIn, compressingStream.TotalBytesOut);
			}

			rc = compressingStream.Deflate(FlushType.Finish);
			Assert.AreEqual(ZlibConstants.Z_STREAM_END, rc, $"at Deflate() [{compressingStream.Message}]");

			rc = compressingStream.EndDeflate();
			Assert.AreEqual(ZlibConstants.Z_OK, rc, $"at EndDeflate() [{compressingStream.Message}]");

			TestContext.WriteLine("Final: uncompressed/compressed bytes: ({0,6},{1,6})",
				  compressingStream.TotalBytesIn, compressingStream.TotalBytesOut);

			var decompressingStream = new ZlibCodec(CompressionMode.Decompress);

			decompressingStream.InputBuffer = compressedBytes;
			decompressingStream.NextIn = 0;
			decompressingStream.AvailableBytesIn = bufferSize;

			// upon inflating, we overwrite the decompressedBytes buffer repeatedly
			var nCycles = 0;
			while (true)
			{
				decompressingStream.OutputBuffer = workBuffer;
				decompressingStream.NextOut = 0;
				decompressingStream.AvailableBytesOut = workBuffer.Length;
				rc = decompressingStream.Inflate(FlushType.None);

				nCycles++;

				if (rc == ZlibConstants.Z_STREAM_END)
					break;

				Assert.AreEqual(ZlibConstants.Z_OK, rc, $"at Inflate() [{decompressingStream.Message}] TotalBytesOut={decompressingStream.TotalBytesOut}");
			}

			rc = decompressingStream.EndInflate();
			Assert.AreEqual(ZlibConstants.Z_OK, rc, $"at EndInflate() [{decompressingStream.Message}]");

			Assert.AreEqual(4 * workBuffer.Length, (int)decompressingStream.TotalBytesOut);

			TestContext.WriteLine("compressed length: {0}", compressingStream.TotalBytesOut);
			TestContext.WriteLine("decompressed length (expected): {0}", 4 * workBuffer.Length);
			TestContext.WriteLine("decompressed length (actual)  : {0}", decompressingStream.TotalBytesOut);
			TestContext.WriteLine("decompression cycles: {0}", nCycles);
		}



		[TestMethod]
		public void Zlib_CompressString()
		{
			TestContext.WriteLine("Original.Length: {0}", GoPlacidly.Length);
			var compressed = ZlibStream.CompressString(GoPlacidly);
			TestContext.WriteLine("compressed.Length: {0}", compressed.Length);
			Assert.IsTrue(compressed.Length < GoPlacidly.Length);

			var uncompressed = ZlibStream.UncompressString(compressed);
			Assert.AreEqual(GoPlacidly.Length, uncompressed.Length);
		}

		[TestMethod]
		public void GZip_CompressString()
		{
			TestContext.WriteLine("Original.Length: {0}", GoPlacidly.Length);
			var compressed = GZipStream.CompressString(GoPlacidly);
			TestContext.WriteLine("compressed.Length: {0}", compressed.Length);
			Assert.IsTrue(compressed.Length < GoPlacidly.Length);

			var uncompressed = GZipStream.UncompressString(compressed);
			Assert.AreEqual(GoPlacidly.Length, uncompressed.Length);
		}

		[TestMethod]
		public void Deflate_CompressString()
		{
			TestContext.WriteLine("Original.Length: {0}", GoPlacidly.Length);
			var compressed = DeflateStream.CompressString(GoPlacidly);
			TestContext.WriteLine("compressed.Length: {0}", compressed.Length);
			Assert.IsTrue(compressed.Length < GoPlacidly.Length);

			var uncompressed = DeflateStream.UncompressString(compressed);
			Assert.AreEqual(GoPlacidly.Length, uncompressed.Length);
		}



		[TestMethod]
		public void Zlib_ZlibStream_CompressWhileWriting()
		{
            MemoryStream msSinkCompressed;
            MemoryStream msSinkDecompressed;
			ZlibStream zOut;

			// first, compress:
			msSinkCompressed = new MemoryStream();
			zOut = new ZlibStream(msSinkCompressed, CompressionMode.Compress, CompressionLevel.BestCompression, true);
			CopyStream(StringToMemoryStream(IhaveaDream), zOut);
			zOut.Close();

			// at this point, msSinkCompressed contains the compressed bytes

			// now, decompress:
			msSinkDecompressed = new MemoryStream();
			zOut = new ZlibStream(msSinkDecompressed, CompressionMode.Decompress);
			msSinkCompressed.Position = 0;
			CopyStream(msSinkCompressed, zOut);

			var result = MemoryStreamToString(msSinkDecompressed);
			TestContext.WriteLine("decompressed: {0}", result);
			Assert.AreEqual(IhaveaDream, result);
		}



		[TestMethod]
		public void Zlib_ZlibStream_CompressWhileReading_wi8557()
		{
            // workitem 8557
            MemoryStream msSinkCompressed;
            MemoryStream msSinkDecompressed;

			// first, compress:
			msSinkCompressed = new MemoryStream();
			var zIn = new ZlibStream(StringToMemoryStream(WhatWouldThingsHaveBeenLike),
										   CompressionMode.Compress,
										   CompressionLevel.BestCompression,
										   true);
			CopyStream(zIn, msSinkCompressed);

			// At this point, msSinkCompressed contains the compressed bytes.
			// Now, decompress:
			msSinkDecompressed = new MemoryStream();
			var zOut = new ZlibStream(msSinkDecompressed, CompressionMode.Decompress);
			msSinkCompressed.Position = 0;
			CopyStream(msSinkCompressed, zOut);

			var result = MemoryStreamToString(msSinkDecompressed);
			TestContext.WriteLine("decompressed: {0}", result);
			Assert.AreEqual(WhatWouldThingsHaveBeenLike, result);
		}



		[TestMethod]
		public void Zlib_CodecTest()
		{
			var sz = rnd.Next(50000) + 50000;
			var fileName = Path.Combine(TopLevelDir, "Zlib_CodecTest.txt");
			CreateAndFillFileText(fileName, sz);

			var UncompressedBytes = File.ReadAllBytes(fileName);

			foreach (CompressionLevel level in Enum.GetValues(typeof(CompressionLevel)))
			{
				TestContext.WriteLine("\n\n+++++++++++++++++++++++++++++++++++++++++++++++++++++++");
				TestContext.WriteLine("trying compression level '{0}'", level.ToString());
				var CompressedBytes = DeflateBuffer(UncompressedBytes, level);
				var DecompressedBytes = InflateBuffer(CompressedBytes, UncompressedBytes.Length);
				CompareBuffers(UncompressedBytes, DecompressedBytes);
			}
			System.Threading.Thread.Sleep(2000);
		}


#if UNNECESSARY
        private byte[] ReadFile(string f)
        {
            System.IO.FileInfo fi = new System.IO.FileInfo(f);
            byte[] buffer = new byte[fi.Length];

            using (var readStream = System.IO.File.OpenRead(f))
            {
                readStream.Read(buffer, 0, buffer.Length);
            }
            return buffer;
        }
#endif



		private byte[] InflateBuffer(byte[] b, int length)
		{
			var bufferSize = 1024;
			var buffer = new byte[bufferSize];
			var decompressor = new ZlibCodec();
			var DecompressedBytes = new byte[length];
			TestContext.WriteLine("\n============================================");
			TestContext.WriteLine("Size of Buffer to Inflate: {0} bytes.", b.Length);
			var ms = new MemoryStream(DecompressedBytes);

			var rc = decompressor.InitializeInflate();

			decompressor.InputBuffer = b;
			decompressor.NextIn = 0;
			decompressor.AvailableBytesIn = b.Length;

			decompressor.OutputBuffer = buffer;

			for (var pass = 0; pass < 2; pass++)
			{
				var flush = (pass == 0)
					? FlushType.None
					: FlushType.Finish;
				do
				{
					decompressor.NextOut = 0;
					decompressor.AvailableBytesOut = buffer.Length;
					rc = decompressor.Inflate(flush);

					if (rc != ZlibConstants.Z_OK && rc != ZlibConstants.Z_STREAM_END)
						throw new Exception("inflating: " + decompressor.Message);

					if (buffer.Length - decompressor.AvailableBytesOut > 0)
						ms.Write(decompressor.OutputBuffer, 0, buffer.Length - decompressor.AvailableBytesOut);
				}
				while (decompressor.AvailableBytesIn > 0 || decompressor.AvailableBytesOut == 0);
			}

			decompressor.EndInflate();
			TestContext.WriteLine("TBO({0}).", decompressor.TotalBytesOut);
			return DecompressedBytes;
		}




		private void CompareBuffers(byte[] a, byte[] b)
		{
			TestContext.WriteLine("\n============================================");
			TestContext.WriteLine("Comparing...");

			if (a.Length != b.Length)
				throw new Exception($"not equal size ({a.Length}!={b.Length})");

			for (var i = 0; i < a.Length; i++)
			{
				if (a[i] != b[i])
					throw new Exception("not equal");
			}
		}



		private byte[] DeflateBuffer(byte[] b, CompressionLevel level)
		{
			var bufferSize = 1024;
			var buffer = new byte[bufferSize];
			var compressor = new ZlibCodec();

			TestContext.WriteLine("\n============================================");
			TestContext.WriteLine("Size of Buffer to Deflate: {0} bytes.", b.Length);
			var ms = new MemoryStream();

			var rc = compressor.InitializeDeflate(level);

			compressor.InputBuffer = b;
			compressor.NextIn = 0;
			compressor.AvailableBytesIn = b.Length;

			compressor.OutputBuffer = buffer;

			for (var pass = 0; pass < 2; pass++)
			{
				var flush = (pass == 0)
					? FlushType.None
					: FlushType.Finish;
				do
				{
					compressor.NextOut = 0;
					compressor.AvailableBytesOut = buffer.Length;
					rc = compressor.Deflate(flush);

					if (rc != ZlibConstants.Z_OK && rc != ZlibConstants.Z_STREAM_END)
						throw new Exception("deflating: " + compressor.Message);

					if (buffer.Length - compressor.AvailableBytesOut > 0)
						ms.Write(compressor.OutputBuffer, 0, buffer.Length - compressor.AvailableBytesOut);
				}
				while (compressor.AvailableBytesIn > 0 || compressor.AvailableBytesOut == 0);
			}

			compressor.EndDeflate();
			Console.WriteLine("TBO({0}).", compressor.TotalBytesOut);

			ms.Seek(0, SeekOrigin.Begin);
			var c = new byte[compressor.TotalBytesOut];
			ms.Read(c, 0, c.Length);
			return c;
		}


		[TestMethod]
		public void Zlib_GZipStream_FileName_And_Comments()
		{
			// select the name of the zip file
			var FileToCompress = Path.Combine(TopLevelDir, "Zlib_GZipStream.dat");
			Assert.IsFalse(File.Exists(FileToCompress), "The temporary zip file '{0}' already exists.", FileToCompress);
			var working = new byte[WORKING_BUFFER_SIZE];
			var n = -1;

			var sz = rnd.Next(21000) + 15000;
			TestContext.WriteLine("  Creating file: {0} sz({1})", FileToCompress, sz);
			CreateAndFillFileText(FileToCompress, sz);

            var fi1 = new FileInfo(FileToCompress);
			var crc1 = DoCrc(FileToCompress);

			// four trials, all combos of FileName and Comment null or not null.
			for (var k = 0; k < 4; k++)
			{
				var CompressedFile = $"{FileToCompress}-{k}.compressed";

				using (Stream input = File.OpenRead(FileToCompress))
				{
					using (var raw = new FileStream(CompressedFile, FileMode.Create))
					{
						using (var compressor =
							   new GZipStream(raw, CompressionMode.Compress, CompressionLevel.BestCompression, true))
						{
							// FileName is optional metadata in the GZip bytestream
							if (k % 2 == 1)
								compressor.FileName = FileToCompress;

							// Comment is optional metadata in the GZip bytestream
							if (k > 2)
								compressor.Comment = "Compressing: " + FileToCompress;

							var buffer = new byte[1024];
							n = -1;
							while (n != 0)
							{
								if (n > 0)
									compressor.Write(buffer, 0, n);

								n = input.Read(buffer, 0, buffer.Length);
							}
						}
					}
				}

                var fi2 = new FileInfo(CompressedFile);

				Assert.IsTrue(fi1.Length > fi2.Length, $"Compressed File is not smaller, trial {k} ({fi1.Length}!>{fi2.Length})");


				// decompress twice:
				// once with System.IO.Compression.GZipStream and once with Ionic.Zlib.GZipStream
				for (var j = 0; j < 2; j++)
				{
					using var input = File.OpenRead(CompressedFile);
					Stream decompressor = null;
					try
					{
						switch (j)
						{
							case 0:
								decompressor = new GZipStream(input, CompressionMode.Decompress, true);
								break;
							case 1:
								decompressor = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress, true);
								break;
						}

						var DecompressedFile =
							$"{CompressedFile}.{((j == 0) ? "Ionic" : "BCL")}.decompressed";

						TestContext.WriteLine("........{0} ...", Path.GetFileName(DecompressedFile));

						using (var s2 = File.Create(DecompressedFile))
						{
							n = -1;
							while (n != 0)
							{
								n = decompressor.Read(working, 0, working.Length);
								if (n > 0)
									s2.Write(working, 0, n);
							}
						}

						var crc2 = DoCrc(DecompressedFile);
						Assert.AreEqual(crc1, crc2);

					}
					finally
					{
						decompressor?.Dispose();
					}
				}
			}
		}


		[TestMethod]
		public void Zlib_GZipStream_ByteByByte_CheckCrc()
		{
			// select the name of the zip file
			var FileToCompress = Path.Combine(TopLevelDir, "Zlib_GZipStream_ByteByByte.dat");
			Assert.IsFalse(File.Exists(FileToCompress), "The temporary zip file '{0}' already exists.", FileToCompress);
			var working = new byte[WORKING_BUFFER_SIZE];
			var n = -1;

			var sz = rnd.Next(21000) + 15000;
			TestContext.WriteLine("  Creating file: {0} sz({1})", FileToCompress, sz);
			CreateAndFillFileText(FileToCompress, sz);

            var fi1 = new FileInfo(FileToCompress);
			var crc1 = DoCrc(FileToCompress);

			// four trials, all combos of FileName and Comment null or not null.
			for (var k = 0; k < 4; k++)
			{
				var CompressedFile = $"{FileToCompress}-{k}.compressed";

				using (Stream input = File.OpenRead(FileToCompress))
				{
					using (var raw = new FileStream(CompressedFile, FileMode.Create))
					{
						using (var compressor =
							   new GZipStream(raw, CompressionMode.Compress, CompressionLevel.BestCompression, true))
						{
							// FileName is optional metadata in the GZip bytestream
							if (k % 2 == 1)
								compressor.FileName = FileToCompress;

							// Comment is optional metadata in the GZip bytestream
							if (k > 2)
								compressor.Comment = "Compressing: " + FileToCompress;

							var buffer = new byte[1024];
							n = -1;
							while (n != 0)
							{
								if (n > 0)
								{
									for (var i = 0; i < n; i++)
										compressor.WriteByte(buffer[i]);
								}

								n = input.Read(buffer, 0, buffer.Length);
							}
						}
					}
				}

                var fi2 = new FileInfo(CompressedFile);

				Assert.IsTrue(fi1.Length > fi2.Length, $"Compressed File is not smaller, trial {k} ({fi1.Length}!>{fi2.Length})");


				// decompress twice:
				// once with System.IO.Compression.GZipStream and once with Ionic.Zlib.GZipStream
				for (var j = 0; j < 2; j++)
				{
					using var input = File.OpenRead(CompressedFile);
					Stream decompressor = null;
					try
					{
						switch (j)
						{
							case 0:
								decompressor = new GZipStream(input, CompressionMode.Decompress, true);
								break;
							case 1:
								decompressor = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress, true);
								break;
						}

						var DecompressedFile =
							$"{CompressedFile}.{((j == 0) ? "Ionic" : "BCL")}.decompressed";

						TestContext.WriteLine("........{0} ...", Path.GetFileName(DecompressedFile));

						using (var s2 = File.Create(DecompressedFile))
						{
							n = -1;
							while (n != 0)
							{
								n = decompressor.Read(working, 0, working.Length);
								if (n > 0)
									s2.Write(working, 0, n);
							}
						}

						var crc2 = DoCrc(DecompressedFile);
						Assert.AreEqual(crc1, crc2);

					}
					finally
					{
						if (decompressor as GZipStream != null)
						{
							var gz = (GZipStream)decompressor;
							gz.Close(); // sets the final CRC
							Assert.AreEqual(gz.Crc32, crc1);
						}

						decompressor?.Dispose();
					}
				}
			}
		}


		[TestMethod]
		public void Zlib_GZipStream_DecompressEmptyStream()
		{
			_DecompressEmptyStream(typeof(GZipStream));
		}


		[TestMethod]
		public void Zlib_ZlibStream_DecompressEmptyStream()
		{
			_DecompressEmptyStream(typeof(ZlibStream));
		}

		private void _DecompressEmptyStream(Type t)
		{
			var working = new byte[WORKING_BUFFER_SIZE];

			// once politely, and the 2nd time through, try to read after EOF
			for (var m = 0; m < 2; m++)
			{
				using var ms1 = new MemoryStream();
				object[] args = { ms1, CompressionMode.Decompress, false };
				using var decompressor = (Stream)Activator.CreateInstance(t, args);
				using var ms2 = new MemoryStream();
				var n = -1;
				while (n != 0)
				{
					n = decompressor.Read(working, 0, working.Length);
					if (n > 0)
						ms2.Write(working, 0, n);
				}

				// we know there is no more data.  Want to insure it does
				// not throw.
				if (m == 1)
					n = decompressor.Read(working, 0, working.Length);


				Assert.AreEqual(ms2.Length, 0L);
			}
		}


		[TestMethod]
		public void Zlib_DeflateStream_InMemory()
		{
            var TextToCompress = UntilHeExtends;

			CompressionLevel[] levels = {CompressionLevel.Level0,
										 CompressionLevel.Level1,
										 CompressionLevel.Default,
										 CompressionLevel.Level7,
										 CompressionLevel.BestCompression};

			// compress with various Ionic levels, and System.IO.Compression (default level)
			for (var k = 0; k < levels.Length + 1; k++)
			{
				var ms = new MemoryStream();

				Stream compressor = null;
				if (k == levels.Length)

					compressor = new System.IO.Compression.DeflateStream(ms, System.IO.Compression.CompressionMode.Compress, false);
				else
				{
					compressor = new DeflateStream(ms, CompressionMode.Compress, levels[k], false);
					TestContext.WriteLine("using level: {0}", levels[k].ToString());
				}

				TestContext.WriteLine("Text to compress is {0} bytes: '{1}'",
									  TextToCompress.Length, TextToCompress);
				TestContext.WriteLine("using compressor: {0}", compressor.GetType().FullName);

				var sw = new StreamWriter(compressor, Encoding.ASCII);
				sw.Write(TextToCompress);
				sw.Close();

				var a = ms.ToArray();
				TestContext.WriteLine("Compressed stream is {0} bytes long", a.Length);

				// de-compress with both Ionic and System.IO.Compression
				for (var j = 0; j < 2; j++)
				{
					var slow = new MySlowMemoryStream(a); // want to force EOF
					Stream decompressor = null;

					switch (j)
					{
						case 0:
							decompressor = new DeflateStream(slow, CompressionMode.Decompress, false);
							break;
						case 1:
							decompressor = new System.IO.Compression.DeflateStream(slow, System.IO.Compression.CompressionMode.Decompress, false);
							break;
					}

					TestContext.WriteLine("using decompressor: {0}", decompressor.GetType().FullName);

					var sr = new StreamReader(decompressor, Encoding.ASCII);
					var DecompressedText = sr.ReadToEnd();

					TestContext.WriteLine("Read {0} characters: '{1}'", DecompressedText.Length, DecompressedText);
					TestContext.WriteLine("\n");
					Assert.AreEqual(TextToCompress, DecompressedText);
				}
			}
		}



		[TestMethod]
		public void Zlib_CloseTwice()
		{
			var TextToCompress = LetMeDoItNow;

			for (var i = 0; i < 3; i++)
			{
				var ms1 = new MemoryStream();

				Stream compressor = null;
				switch (i)
				{
					case 0:
						compressor = new DeflateStream(ms1, CompressionMode.Compress, CompressionLevel.BestCompression, false);
						break;
					case 1:
						compressor = new GZipStream(ms1, CompressionMode.Compress, false);
						break;
					case 2:
						compressor = new ZlibStream(ms1, CompressionMode.Compress, false);
						break;
				}

				TestContext.WriteLine("Text to compress is {0} bytes: '{1}'",
									  TextToCompress.Length, TextToCompress);
				TestContext.WriteLine("using compressor: {0}", compressor.GetType().FullName);

				var sw = new StreamWriter(compressor, Encoding.ASCII);
				sw.Write(TextToCompress);
				sw.Close(); // implicitly closes compressor
				sw.Close();// implicitly closes compressor, again

				compressor.Close(); // explicitly closes compressor
				var a = ms1.ToArray();
				TestContext.WriteLine("Compressed stream is {0} bytes long", a.Length);

				var ms2 = new MemoryStream(a);
				Stream decompressor = null;

				switch (i)
				{
					case 0:
						decompressor = new DeflateStream(ms2, CompressionMode.Decompress, false);
						break;
					case 1:
						decompressor = new GZipStream(ms2, CompressionMode.Decompress, false);
						break;
					case 2:
						decompressor = new ZlibStream(ms2, CompressionMode.Decompress, false);
						break;
				}

				TestContext.WriteLine("using decompressor: {0}", decompressor.GetType().FullName);

				var sr = new StreamReader(decompressor, Encoding.ASCII);
				var DecompressedText = sr.ReadToEnd();

				// verify that multiple calls to Close() do not throw
				sr.Close();
				sr.Close();
				decompressor.Close();

				TestContext.WriteLine("Read {0} characters: '{1}'", DecompressedText.Length, DecompressedText);
				TestContext.WriteLine("\n");
				Assert.AreEqual(TextToCompress, DecompressedText);
			}
		}


		[TestMethod]
		[ExpectedException(typeof(ObjectDisposedException))]
		public void Zlib_DisposedException_DeflateStream()
		{
			var TextToCompress = LetMeDoItNow;

			var ms1 = new MemoryStream();

			Stream compressor = new DeflateStream(ms1, CompressionMode.Compress, false);

			TestContext.WriteLine("Text to compress is {0} bytes: '{1}'",
								  TextToCompress.Length, TextToCompress);
			TestContext.WriteLine("using compressor: {0}", compressor.GetType().FullName);

			var sw = new StreamWriter(compressor, Encoding.ASCII);
			sw.Write(TextToCompress);
			sw.Close(); // implicitly closes compressor
			sw.Close(); // implicitly closes compressor, again

			compressor.Close(); // explicitly closes compressor
			var a = ms1.ToArray();
			TestContext.WriteLine("Compressed stream is {0} bytes long", a.Length);

			var ms2 = new MemoryStream(a);
			Stream decompressor = new DeflateStream(ms2, CompressionMode.Decompress, false);

			TestContext.WriteLine("using decompressor: {0}", decompressor.GetType().FullName);

			var sr = new StreamReader(decompressor, Encoding.ASCII);
			var DecompressedText = sr.ReadToEnd();
			sr.Close();

			TestContext.WriteLine("decompressor.CanRead = {0}", decompressor.CanRead);

			TestContext.WriteLine("Read {0} characters: '{1}'", DecompressedText.Length, DecompressedText);
			TestContext.WriteLine("\n");
			Assert.AreEqual(TextToCompress, DecompressedText);

		}


		[TestMethod]
		[ExpectedException(typeof(ObjectDisposedException))]
		public void Zlib_DisposedException_GZipStream()
		{
			var TextToCompress = IhaveaDream;

			var ms1 = new MemoryStream();

			Stream compressor = new GZipStream(ms1, CompressionMode.Compress, false);

			TestContext.WriteLine("Text to compress is {0} bytes: '{1}'",
								  TextToCompress.Length, TextToCompress);
			TestContext.WriteLine("using compressor: {0}", compressor.GetType().FullName);

			var sw = new StreamWriter(compressor, Encoding.ASCII);
			sw.Write(TextToCompress);
			sw.Close(); // implicitly closes compressor
			sw.Close(); // implicitly closes compressor, again

			compressor.Close(); // explicitly closes compressor
			var a = ms1.ToArray();
			TestContext.WriteLine("Compressed stream is {0} bytes long", a.Length);

			var ms2 = new MemoryStream(a);
			Stream decompressor = new GZipStream(ms2, CompressionMode.Decompress, false);

			TestContext.WriteLine("using decompressor: {0}", decompressor.GetType().FullName);

			var sr = new StreamReader(decompressor, Encoding.ASCII);
			var DecompressedText = sr.ReadToEnd();
			sr.Close();

			TestContext.WriteLine("decompressor.CanRead = {0}", decompressor.CanRead);

			TestContext.WriteLine("Read {0} characters: '{1}'", DecompressedText.Length, DecompressedText);
			TestContext.WriteLine("\n");
			Assert.AreEqual(TextToCompress, DecompressedText);
		}


		[TestMethod]
		[ExpectedException(typeof(ObjectDisposedException))]
		public void Zlib_DisposedException_ZlibStream()
		{
			var TextToCompress = IhaveaDream;

			var ms1 = new MemoryStream();

			Stream compressor = new ZlibStream(ms1, CompressionMode.Compress, false);

			TestContext.WriteLine("Text to compress is {0} bytes: '{1}'",
								  TextToCompress.Length, TextToCompress);
			TestContext.WriteLine("using compressor: {0}", compressor.GetType().FullName);

			var sw = new StreamWriter(compressor, Encoding.ASCII);
			sw.Write(TextToCompress);
			sw.Close(); // implicitly closes compressor
			sw.Close(); // implicitly closes compressor, again

			compressor.Close(); // explicitly closes compressor
			var a = ms1.ToArray();
			TestContext.WriteLine("Compressed stream is {0} bytes long", a.Length);

			var ms2 = new MemoryStream(a);
			Stream decompressor = new ZlibStream(ms2, CompressionMode.Decompress, false);

			TestContext.WriteLine("using decompressor: {0}", decompressor.GetType().FullName);

			var sr = new StreamReader(decompressor, Encoding.ASCII);
			var DecompressedText = sr.ReadToEnd();
			sr.Close();

			TestContext.WriteLine("decompressor.CanRead = {0}", decompressor.CanRead);

			TestContext.WriteLine("Read {0} characters: '{1}'", DecompressedText.Length, DecompressedText);
			TestContext.WriteLine("\n");
			Assert.AreEqual(TextToCompress, DecompressedText);
		}




		[TestMethod]
		public void Zlib_Streams_VariousSizes()
		{
			var working = new byte[WORKING_BUFFER_SIZE];
			var n = -1;
            int[] Sizes = { 8000, 88000, 188000, 388000, 580000, 1580000 };

			for (var p = 0; p < Sizes.Length; p++)
			{
				// both binary and text files
				for (var m = 0; m < 2; m++)
				{
					var sz = rnd.Next(Sizes[p]) + Sizes[p];
					var FileToCompress = Path.Combine(TopLevelDir, $"Zlib_Streams.{sz}.{((m == 0) ? "txt" : "bin")}");
					Assert.IsFalse(File.Exists(FileToCompress), "The temporary file '{0}' already exists.", FileToCompress);
					TestContext.WriteLine("Creating file {0}   {1} bytes", FileToCompress, sz);
					if (m == 0)
						CreateAndFillFileText(FileToCompress, sz);
					else
						_CreateAndFillBinary(FileToCompress, sz, false);

					var crc1 = DoCrc(FileToCompress);
					TestContext.WriteLine("Initial CRC: 0x{0:X8}", crc1);

					// try both GZipStream and DeflateStream
					for (var k = 0; k < 2; k++)
					{
						// compress with Ionic and System.IO.Compression
						for (var i = 0; i < 2; i++)
						{
							var CompressedFileRoot = $"{FileToCompress}.{((k == 0) ? "GZIP" : "DEFLATE")}.{((i == 0) ? "Ionic" : "BCL")}.compressed";

							var x = k + i * 2;
							var z = (x == 0) ? 4 : 1;
							// why 4 trials??   (only for GZIP and Ionic)
							for (var h = 0; h < z; h++)
							{
								var CompressedFile = (x == 0)
									? CompressedFileRoot + ".trial" + h
									: CompressedFileRoot;

								using (var input = File.OpenRead(FileToCompress))
								{
									using (var raw = File.Create(CompressedFile))
									{
										Stream compressor = null;
										try
										{
											switch (x)
											{
												case 0: // k == 0, i == 0
													compressor = new GZipStream(raw, CompressionMode.Compress, true);
													break;
												case 1: // k == 1, i == 0
													compressor = new DeflateStream(raw, CompressionMode.Compress, true);
													break;
												case 2: // k == 0, i == 1
													compressor = new System.IO.Compression.GZipStream(raw, System.IO.Compression.CompressionMode.Compress, true);
													break;
												case 3: // k == 1, i == 1
													compressor = new System.IO.Compression.DeflateStream(raw, System.IO.Compression.CompressionMode.Compress, true);
													break;
											}
											//TestContext.WriteLine("Compress with: {0} ..", compressor.GetType().FullName);

											TestContext.WriteLine("........{0} ...", Path.GetFileName(CompressedFile));

											if (x == 0)
											{
												if (h != 0)
												{
                                                    var gzip = compressor as GZipStream;

													if (h % 2 == 1)
														gzip.FileName = FileToCompress;

													if (h > 2)
														gzip.Comment = "Compressing: " + FileToCompress;

												}
											}

											n = -1;
											while ((n = input.Read(working, 0, working.Length)) != 0)
											{
												compressor.Write(working, 0, n);
											}

										}
										finally
										{
											compressor?.Dispose();
										}
									}
								}

								// now, decompress with Ionic and System.IO.Compression
								// for (int j = 0; j < 2; j++)
								for (var j = 1; j >= 0; j--)
								{
									using var input = File.OpenRead(CompressedFile);
									Stream decompressor = null;
									try
									{
										var w = k + j * 2;
										switch (w)
										{
											case 0: // k == 0, j == 0
												decompressor = new GZipStream(input, CompressionMode.Decompress, true);
												break;
											case 1: // k == 1, j == 0
												decompressor = new DeflateStream(input, CompressionMode.Decompress, true);
												break;
											case 2: // k == 0, j == 1
												decompressor = new System.IO.Compression.GZipStream(input, System.IO.Compression.CompressionMode.Decompress, true);
												break;
											case 3: // k == 1, j == 1
												decompressor = new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress, true);
												break;
										}

										//TestContext.WriteLine("Decompress: {0} ...", decompressor.GetType().FullName);
										var DecompressedFile =
											$"{CompressedFile}.{((j == 0) ? "Ionic" : "BCL")}.decompressed";

										TestContext.WriteLine("........{0} ...", Path.GetFileName(DecompressedFile));

										using (var s2 = File.Create(DecompressedFile))
										{
											n = -1;
											while (n != 0)
											{
												n = decompressor.Read(working, 0, working.Length);
												if (n > 0)
													s2.Write(working, 0, n);
											}
										}

										var crc2 = DoCrc(DecompressedFile);
										Assert.AreEqual((uint)crc1, (uint)crc2);

									}
									finally
									{
										decompressor?.Dispose();
									}
								}
							}
						}
					}
				}
			}
			TestContext.WriteLine("Done.");
		}




		private void PerformTrialWi8870(byte[] buffer)
		{
			TestContext.WriteLine("Original");

			byte[] compressedBytes = null;
			using (var ms1 = new MemoryStream())
			{
				using (var compressor = new DeflateStream(ms1, CompressionMode.Compress, false))
				{
					compressor.Write(buffer, 0, buffer.Length);
				}
				compressedBytes = ms1.ToArray();
			}

			TestContext.WriteLine("Compressed {0} bytes into {1} bytes",
								  buffer.Length, compressedBytes.Length);

			byte[] decompressed = null;
			using (var ms2 = new MemoryStream())
			{
				using (var deflateStream = new DeflateStream(ms2, CompressionMode.Decompress, false))
				{
					deflateStream.Write(compressedBytes, 0, compressedBytes.Length);
				}
				decompressed = ms2.ToArray();
			}

			TestContext.WriteLine("Decompressed");


			var check = true;
			if (buffer.Length != decompressed.Length)
			{
				TestContext.WriteLine("Different lengths.");
				check = false;
			}
			else
			{
				for (var i = 0; i < buffer.Length; i++)
				{
					if (buffer[i] != decompressed[i])
					{
						TestContext.WriteLine("byte {0} differs", i);
						check = false;
						break;
					}
				}
			}

			Assert.IsTrue(check, "Data check failed.");
		}




		private byte[] RandomizeBuffer(int length)
		{
			var buffer = new byte[length];
			var mod1 = 86 + rnd.Next(46) / 2 + 1;
			var mod2 = 50 + rnd.Next(72) / 2 + 1;
			for (var i = 0; i < length; i++)
			{
				if (i > 200)
					buffer[i] = (byte)(i % mod1);
				else if (i > 100)
					buffer[i] = (byte)(i % mod2);
				else if (i > 42)
					buffer[i] = (byte)(i % 33);
				else buffer[i] = (byte)i;
			}
			return buffer;
		}



		[TestMethod]
		public void Zlib_DeflateStream_wi8870()
		{
			for (var j = 0; j < 1000; j++)
			{
				var buffer = RandomizeBuffer(117 + rnd.Next(3) * 100);
				PerformTrialWi8870(buffer);
			}
		}










		private int DoCrc(string filename)
		{
			using Stream a = File.OpenRead(filename);
			using var crc = new Crc.CrcCalculatorStream(a);
			var working = new byte[WORKING_BUFFER_SIZE];
			var n = -1;
			while (n != 0)
				n = crc.Read(working, 0, working.Length);
			return crc.Crc;
		}



		private static void _CreateAndFillBinary(string Filename, long size, bool zeroes)
		{
            var bytesRemaining = size;
            var rnd = new Random();
			// fill with binary data
			var Buffer = new byte[20000];
			using Stream fileStream = new FileStream(Filename, FileMode.Create, FileAccess.Write);
			while (bytesRemaining > 0)
			{
				var sizeOfChunkToWrite = (bytesRemaining > Buffer.Length) ? Buffer.Length : (int)bytesRemaining;
				if (!zeroes) rnd.NextBytes(Buffer);
				fileStream.Write(Buffer, 0, sizeOfChunkToWrite);
				bytesRemaining -= sizeOfChunkToWrite;
			}
			fileStream.Close();
		}


		internal static void CreateAndFillFileText(string Filename, long size)
		{
            var bytesRemaining = size;
            var rnd = new Random();
			// fill the file with text data
			using var sw = File.CreateText(Filename);
			do
			{
				// pick a word at random
				var selectedWord = LoremIpsumWords[rnd.Next(LoremIpsumWords.Length)];
				if (bytesRemaining < selectedWord.Length + 1)
				{
					sw.Write(selectedWord.Substring(0, (int)bytesRemaining));
					bytesRemaining = 0;
				}
				else
				{
					sw.Write(selectedWord);
					sw.Write(" ");
					bytesRemaining -= selectedWord.Length + 1;
				}
			} while (bytesRemaining > 0);
			sw.Close();
		}

		[TestMethod]
		public void TestAdler32()
		{
			// create a buffer full of 0xff's
			var buffer = new byte[2048 * 4];
			for (var i = 0; i < buffer.Length; i++)
			{
				buffer[i] = 255;
			};

			var goal = 4104380882;
			var testAdler = new Action<int>(chunk =>
			{
				var index = 0;
				var adler = Adler.Adler32(0, null, 0, 0);
				while (index < buffer.Length)
				{
					var length = Math.Min(buffer.Length - index, chunk);
					adler = Adler.Adler32(adler, buffer, index, length);
					index = index + chunk;
				}
				Assert.AreEqual(adler, goal);
			});

			testAdler(3979);
			testAdler(3980);
			testAdler(3999);
		}



		internal static string LetMeDoItNow = "I expect to pass through the world but once. Any good therefore that I can do, or any kindness I can show to any creature, let me do it now. Let me not defer it, for I shall not pass this way again. -- Anonymous, although some have attributed it to Stephen Grellet";

		internal static string UntilHeExtends = "Until he extends the circle of his compassion to all living things, man will not himself find peace. - Albert Schweitzer, early 20th-century German Nobel Peace Prize-winning mission doctor and theologian.";

		internal static string WhatWouldThingsHaveBeenLike = "'What would things have been like [in Russia] if during periods of mass arrests people had not simply sat there, paling with terror at every bang on the downstairs door and at every step on the staircase, but understood they had nothing to lose and had boldly set up in the downstairs hall an ambush of half a dozen people?' -- Alexander Solzhenitsyn";

		internal static string GoPlacidly =
			@"Go placidly amid the noise and haste, and remember what peace there may be in silence.

As far as possible, without surrender, be on good terms with all persons. Speak your truth quietly and clearly; and listen to others, even to the dull and the ignorant, they too have their story. Avoid loud and aggressive persons, they are vexations to the spirit.

If you compare yourself with others, you may become vain and bitter; for always there will be greater and lesser persons than yourself. Enjoy your achievements as well as your plans. Keep interested in your own career, however humble; it is a real possession in the changing fortunes of time.

Exercise caution in your business affairs, for the world is full of trickery. But let this not blind you to what virtue there is; many persons strive for high ideals, and everywhere life is full of heroism. Be yourself. Especially, do not feign affection. Neither be cynical about love, for in the face of all aridity and disenchantment it is perennial as the grass.

Take kindly to the counsel of the years, gracefully surrendering the things of youth. Nurture strength of spirit to shield you in sudden misfortune. But do not distress yourself with imaginings. Many fears are born of fatigue and loneliness.

Beyond a wholesome discipline, be gentle with yourself. You are a child of the universe, no less than the trees and the stars; you have a right to be here. And whether or not it is clear to you, no doubt the universe is unfolding as it should.

Therefore be at peace with God, whatever you conceive Him to be, and whatever your labors and aspirations, in the noisy confusion of life, keep peace in your soul.

With all its sham, drudgery and broken dreams, it is still a beautiful world.

Be cheerful. Strive to be happy.

Max Ehrmann c.1920
";


		internal static string IhaveaDream = @"Let us not wallow in the valley of despair, I say to you today, my friends.

And so even though we face the difficulties of today and tomorrow, I still have a dream. It is a dream deeply rooted in the American dream.

I have a dream that one day this nation will rise up and live out the true meaning of its creed: 'We hold these truths to be self-evident, that all men are created equal.'

I have a dream that one day on the red hills of Georgia, the sons of former slaves and the sons of former slave owners will be able to sit down together at the table of brotherhood.

I have a dream that one day even the state of Mississippi, a state sweltering with the heat of injustice, sweltering with the heat of oppression, will be transformed into an oasis of freedom and justice.

I have a dream that my four little children will one day live in a nation where they will not be judged by the color of their skin but by the content of their character.

I have a dream today!

I have a dream that one day, down in Alabama, with its vicious racists, with its governor having his lips dripping with the words of 'interposition' and 'nullification' -- one day right there in Alabama little black boys and black girls will be able to join hands with little white boys and white girls as sisters and brothers.

I have a dream today!

I have a dream that one day every valley shall be exalted, and every hill and mountain shall be made low, the rough places will be made plain, and the crooked places will be made straight; 'and the glory of the Lord shall be revealed and all flesh shall see it together.'2
";

		internal static string LoremIpsum =
"Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Integer " +
"vulputate, nibh non rhoncus euismod, erat odio pellentesque lacus, sit " +
"amet convallis mi augue et odio. Phasellus cursus urna facilisis " +
"quam. Suspendisse nec metus et sapien scelerisque euismod. Nullam " +
"molestie sem quis nisl. Fusce pellentesque, ante sed semper egestas, sem " +
"nulla vestibulum nulla, quis sollicitudin leo lorem elementum " +
"wisi. Aliquam vestibulum nonummy orci. Sed in dolor sed enim ullamcorper " +
"accumsan. Duis vel nibh. Class aptent taciti sociosqu ad litora torquent " +
"per conubia nostra, per inceptos hymenaeos. Sed faucibus, enim sit amet " +
"venenatis laoreet, nisl elit posuere est, ut sollicitudin tortor velit " +
"ut ipsum. Aliquam erat volutpat. Phasellus tincidunt vehicula " +
"eros. Curabitur vitae erat. " +
"\n " +
"Quisque pharetra lacus quis sapien. Duis id est non wisi sagittis " +
"adipiscing. Nulla facilisi. Etiam quam erat, lobortis eu, facilisis nec, " +
"blandit hendrerit, metus. Fusce hendrerit. Nunc magna libero, " +
"sollicitudin non, vulputate non, ornare id, nulla.  Suspendisse " +
"potenti. Nullam in mauris. Curabitur et nisl vel purus vehicula " +
"sodales. Class aptent taciti sociosqu ad litora torquent per conubia " +
"nostra, per inceptos hymenaeos. Cum sociis natoque penatibus et magnis " +
"dis parturient montes, nascetur ridiculus mus. Donec semper, arcu nec " +
"dignissim porta, eros odio tempus pede, et laoreet nibh arcu et " +
"nisl. Morbi pellentesque eleifend ante. Morbi dictum lorem non " +
"ante. Nullam et augue sit amet sapien varius mollis. " +
"\n " +
"Nulla erat lorem, fringilla eget, ultrices nec, dictum sed, " +
"sapien. Aliquam libero ligula, porttitor scelerisque, lobortis nec, " +
"dignissim eu, elit. Etiam feugiat, dui vitae laoreet faucibus, tellus " +
"urna molestie purus, sit amet pretium lorem pede in erat.  Ut non libero " +
"et sapien porttitor eleifend. Vestibulum ante ipsum primis in faucibus " +
"orci luctus et ultrices posuere cubilia Curae; In at lorem et lacus " +
"feugiat iaculis. Nunc tempus eros nec arcu tristique egestas. Quisque " +
"metus arcu, pretium in, suscipit dictum, bibendum sit amet, " +
"mauris. Aliquam non urna. Suspendisse eget diam. Aliquam erat " +
"volutpat. In euismod aliquam lorem. Mauris dolor nisl, consectetuer sit " +
"amet, suscipit sodales, rutrum in, lorem. Nunc nec nisl. Nulla ante " +
"libero, aliquam porttitor, aliquet at, imperdiet sed, diam. Pellentesque " +
"tincidunt nisl et ipsum. Suspendisse purus urna, semper quis, laoreet " +
"in, vestibulum vel, arcu. Nunc elementum eros nec mauris. " +
"\n " +
"Vivamus congue pede at quam. Aliquam aliquam leo vel turpis. Ut " +
"commodo. Integer tincidunt sem a risus. Cras aliquam libero quis " +
"arcu. Integer posuere. Nulla malesuada, wisi ac elementum sollicitudin, " +
"libero libero molestie velit, eu faucibus est ante eu libero. Sed " +
"vestibulum, dolor ac ultricies consectetuer, tellus risus interdum diam, " +
"a imperdiet nibh eros eget mauris. Donec faucibus volutpat " +
"augue. Phasellus vitae arcu quis ipsum ultrices fermentum. Vivamus " +
"ultricies porta ligula. Nullam malesuada. Ut feugiat urna non " +
"turpis. Vivamus ipsum. Vivamus eleifend condimentum risus. Curabitur " +
"pede. Maecenas suscipit pretium tortor. Integer pellentesque. " +
"\n " +
"Mauris est. Aenean accumsan purus vitae ligula. Lorem ipsum dolor sit " +
"amet, consectetuer adipiscing elit. Nullam at mauris id turpis placerat " +
"accumsan. Sed pharetra metus ut ante. Aenean vel urna sit amet ante " +
"pretium dapibus. Sed nulla. Sed nonummy, lacus a suscipit semper, erat " +
"wisi convallis mi, et accumsan magna elit laoreet sem. Nam leo est, " +
"cursus ut, molestie ac, laoreet id, mauris. Suspendisse auctor nibh. " +
"\n";

		static string[] LoremIpsumWords;

		private const int WORKING_BUFFER_SIZE = 0x4000;

	}


	public class MySlowMemoryStream : MemoryStream
	{
		// ctor
		public MySlowMemoryStream(byte[] bytes) : base(bytes, false) { }

		public override int Read(byte[] buffer, int offset, int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException();

			if (count == 0)
				return 0;

			// force stream to read just one byte at a time
			var NextByte = base.ReadByte();
			if (NextByte == -1)
				return 0;

			buffer[offset] = (byte)NextByte;
			return 1;
		}
	}



}
