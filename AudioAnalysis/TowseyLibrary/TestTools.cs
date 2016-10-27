﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using log4net;
using Acoustics.Shared.Csv;
using System.Diagnostics;

namespace TowseyLibrary
{
    public static class TestTools
    {




        public static void RecognizerScoresTest(string fileName, DirectoryInfo opDir, string testName, double[] scoreArray)
        {
            var testDir = new DirectoryInfo(opDir + $"\\UnitTest_{testName}");
            var benchmarkDir = new DirectoryInfo(testDir + "\\ExpectedOutput");
            if (!benchmarkDir.Exists) benchmarkDir.Create();
            var benchmarkFilePath = Path.Combine(benchmarkDir.FullName, fileName + ".TestScores.csv");
            var testFilePath = Path.Combine(testDir.FullName, fileName + ".Scores.csv");
            FileTools.WriteArray2File(scoreArray, testFilePath);

            LoggedConsole.WriteLine($"# ARRAY TEST: Starting benchmark score array test for the {testName} recognizer:");
            var benchmarkFile = new FileInfo(benchmarkFilePath);
            if (!benchmarkFile.Exists)
            {
                LoggedConsole.WriteWarnLine("   A file of test scores does not exist.    Writing output as future scores-test file");
                FileTools.WriteArray2File(scoreArray, benchmarkFilePath);
            }
            else
            {
                CompareArrayWithBenchmark(testName, scoreArray, new FileInfo(benchmarkFilePath));
            }
        } 


        /// <summary>
        /// This test checks a score array (array of doubles) against a standard or benchmark previously stored.
        /// </summary>
        /// <param name="testName"></param>
        /// <param name="scoreArray"></param>
        /// <param name="scoreFile"></param>
        public static void CompareArrayWithBenchmark(string testName, double[] scoreArray, FileInfo scoreFile)
        {
            LoggedConsole.WriteLine("# TESTING: Starting benchmark test for "+ testName + ":");
            LoggedConsole.WriteLine("#          Comparing passed array of double with content of file <" + scoreFile.Name + ">");
            bool allOk = true;
            var scoreLines = FileTools.ReadTextFile(scoreFile.FullName);
            for (int i = 0; i < scoreLines.Count; i++)
            {
                var str = scoreArray[i].ToString(CultureInfo.InvariantCulture);
                if (!scoreLines[i].Equals(str))
                {
                    LoggedConsole.WriteWarnLine($"Line {i}: {str} NOT= benchmark <{scoreLines[i]}>");
                    allOk = false;
                }
            }
            if (allOk)
            {
                LoggedConsole.WriteSuccessLine("   SUCCESS! Passed the SCORE ARRAY TEST.");
            }
            else
            {
                LoggedConsole.WriteWarnLine("   FAILED THE SCORE ARRAY TEST");
            }
            LoggedConsole.WriteLine("Completed benchmark test.");
        }



        /// <summary>
        /// This test checks two text/csv files to determine if they are the same.
        /// </summary>
        /// <param name="testName"></param>
        /// <param name="file1"></param>
        /// <param name="file2"></param>
        public static void FileEqualityTest(string testName, FileInfo file1, FileInfo file2)
        {
            LoggedConsole.WriteLine("# TESTING: Starting benchmark test for " + testName + ":");
            LoggedConsole.WriteLine("#          Comparing file <"+ file1.Name+ "> with <"+ file2.Name + ">");


            if (!file1.Exists)
            {
                LoggedConsole.WriteWarnLine("   "+testName+"  File1 <"+ file1.Name + "> does not exist.");
                return;
            }
            if (!file2.Exists)
            {
                LoggedConsole.WriteWarnLine("   " + testName + "  File2 <" + file2.Name + "> does not exist.");
                return;
            }
            var lines1 = FileTools.ReadTextFile(file1.FullName);
            var lines2 = FileTools.ReadTextFile(file2.FullName);

            if (lines1.Count == 0)
            {
                LoggedConsole.WriteWarnLine("   " + testName + "  File1 contains zero lines.");
                return;
            }
            if (lines2.Count == 0)
            {
                LoggedConsole.WriteWarnLine("   " + testName + "  File2 contains zero lines.");
                return;
            }

            if (lines1.Count != lines2.Count)
            {
                LoggedConsole.WriteWarnLine("   " + testName + "  The two files do not contain the same number of lines.");
                LoggedConsole.WriteWarnLine("   line count 1 <"+ lines1.Count + ">  !=  line count 2 <" + lines2.Count + ">");
                return;
            }


            var allOk = true;

            for (int i = 0; i < lines2.Count; i++)
            {
                if (! lines1[i].Equals(lines2[i]))
                {
                    LoggedConsole.WriteWarnLine($"Line {i}: <{lines1[i]}>   !=   benchmark <{lines2[i]}>");
                    allOk = false;
                }
            }
            if (allOk)
            {
                LoggedConsole.WriteSuccessLine("   SUCCESS! Passed the FILE EQUALITY TEST.");
            }
            else
            {
                LoggedConsole.WriteWarnLine("   FAILED THE TEST");
            }
            LoggedConsole.WriteLine("Completed benchmark test.");
        }



    }
}