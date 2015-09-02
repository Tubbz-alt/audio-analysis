﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CsvTests.cs" company="QutBioacoustics">
//   All code in this file and all associated files are the copyright of the QUT Bioacoustics Research Group (formally MQUTeR).
// </copyright>
// <summary>
//   Defines the CsvTests type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Acoustics.Test.Shared
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using Acoustics.Shared;
    using Acoustics.Shared.Csv;

    using EcoSounds.Mvc.Tests;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using TowseyLibrary;

    [TestClass]
    public class CsvTests
    {

        private static readonly double[,] TestMatrix =
            {
                { 1.0, 2.0, 3.0, 4.0 },
                { 5.0, 6.0, 7.0, 8.0 },
                { 9.0, 10.0, 11.0, 12.0 },
                { 13.0, 14.0, 15.0, 16.0 },
                { 17.0, 18.0, 19.0, 20.0 }
            };

        private DirectoryInfo outputDirectory;
        private FileInfo testFile;

        [TestInitialize]
        public void Setup()
        {
            this.outputDirectory = TestHelper.GetTempDir();

            this.testFile = TestHelper.GetTempFile(".csv");
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.outputDirectory.Delete();
            this.testFile.Delete();
        }

        [TestMethod]
        public void TestWriteSimpleMatrix()
        {
            Csv.WriteMatrixToCsv(this.testFile, TestMatrix);

            var expected = CsvExpectedHelper(
                new[] { 0, 1, 2, 3 },
                new[] { 4, 5, 6, 7 },
                new[] { 8, 9, 10, 11 },
                new[] { 12, 13, 14, 15 },
                new[] { 16, 17, 18, 19 });

            this.AssertCsvEqual(expected, this.testFile);
        }

        [TestMethod]
        public void TestWriteSimpleMatrixColumnMajor()
        {
            Csv.WriteMatrixToCsv(this.testFile, TestMatrix, TwoDimensionalArray.ColumnMajor);

            var expected = CsvExpectedHelper(
                new[] { 0, 4, 8, 12, 16 },
                new[] { 1, 5, 9, 13, 17 },
                new[] { 2, 6, 10, 14, 18 },
                new[] { 3, 7, 11, 15, 19 });

            this.AssertCsvEqual(expected, this.testFile);
        }

        [TestMethod]
        public void TestWriteSimpleMatrixColumnMajorFlipped()
        {
            Csv.WriteMatrixToCsv(this.testFile, TestMatrix, TwoDimensionalArray.ColumnMajorFlipped);

            var expected = CsvExpectedHelper(
                new[] { 16, 12, 8, 4, 0, },
                new[] { 17, 13, 9, 5, 1, },
                new[] { 18, 14, 10, 6, 2, },
                new[] { 19, 15, 11, 7, 3, });

            this.AssertCsvEqual(expected, this.testFile);
        }

        [TestMethod]
        public void TestWriteSimpleMatrixAlternateName()
        {
            Csv.WriteMatrixToCsv(this.testFile, TestMatrix, TwoDimensionalArray.Normal);

            var expected = CsvExpectedHelper(
                new[] { 0, 1, 2, 3 },
                new[] { 4, 5, 6, 7 },
                new[] { 8, 9, 10, 11 },
                new[] { 12, 13, 14, 15 },
                new[] { 16, 17, 18, 19 });

            this.AssertCsvEqual(expected, this.testFile);
        }

        [TestMethod]
        public void TestWriteSimpleMatrixColumnMajorAlternateName()
        {
            Csv.WriteMatrixToCsv(this.testFile, TestMatrix, TwoDimensionalArray.Transpose);

            var expected = CsvExpectedHelper(
                new[] { 0, 4, 8, 12, 16 },
                new[] { 1, 5, 9, 13, 17 },
                new[] { 2, 6, 10, 14, 18 },
                new[] { 3, 7, 11, 15, 19 });

            this.AssertCsvEqual(expected, this.testFile);
        }

        [TestMethod]
        public void TestWriteSimpleMatrixColumnMajorFlippedAlternateName()
        {
            Csv.WriteMatrixToCsv(this.testFile, TestMatrix, TwoDimensionalArray.Rotate90ClockWise);

            var expected = CsvExpectedHelper(
                new[] { 16, 12, 8, 4, 0, },
                new[] { 17, 13, 9, 5, 1, },
                new[] { 18, 14, 10, 6, 2, },
                new[] { 19, 15, 11, 7, 3, });

            this.AssertCsvEqual(expected, this.testFile);
        }


        [TestMethod]
        public void TestWriteAndReadSimpleMatrix()
        {
            Csv.WriteMatrixToCsv(this.testFile, TestMatrix);

            double[,] matrix = Csv.ReadMatrixFromCsv<double>(this.testFile, TwoDimensionalArray.RowMajor);

            Debug.WriteLine(Json.SerialiseToString(TestMatrix, prettyPrint: true));
            Debug.WriteLine("Actual:");
            Debug.WriteLine(Json.SerialiseToString(matrix, true));

            CollectionAssert.AreEqual(TestMatrix, matrix);
        }

        [TestMethod]
        public void TestWriteAndReadSimpleMatrixColumnMajor()
        {
            Csv.WriteMatrixToCsv(this.testFile, TestMatrix, TwoDimensionalArray.ColumnMajor);

            double[,] matrix = Csv.ReadMatrixFromCsv<double>(this.testFile, TwoDimensionalArray.ColumnMajor);

            CollectionAssert.AreEqual(TestMatrix, matrix);
        }

        [TestMethod]
        public void TestWriteAndReadSimpleMatrixColumnMajorFlipped()
        {
            Csv.WriteMatrixToCsv(this.testFile, TestMatrix, TwoDimensionalArray.ColumnMajorFlipped);

            double[,] matrix = Csv.ReadMatrixFromCsv<double>(this.testFile, TwoDimensionalArray.ColumnMajorFlipped);

            CollectionAssert.AreEqual(TestMatrix, matrix);
        }


        [TestMethod]
        public void TestWriteAndThenReadDifferentOrders()
        {
            Csv.WriteMatrixToCsv(this.testFile, TestMatrix, TwoDimensionalArray.Rotate90ClockWise);

            double[,] matrix = Csv.ReadMatrixFromCsv<double>(this.testFile, TwoDimensionalArray.RowMajor);

            matrix = MatrixTools.MatrixRotate90Anticlockwise(matrix);

            CollectionAssert.AreEqual(TestMatrix, matrix);
        }


        private void AssertCsvEqual(string expected, FileInfo actual)
        {
            var lines = File.ReadAllText(actual.FullName);
            
            Assert.AreEqual(expected, lines);
            Debug.WriteLine(lines);
            CollectionAssert.AreEqual(expected.ToArray(), lines.ToArray());
        }

        private static string CsvExpectedHelper(params int[][] indexes)
        {

            return "Index," + string.Join(",", indexes[0].Select((s, i) => "c00000" + i)) + Environment.NewLine
                   + string.Join(
                       Environment.NewLine,
                       indexes.Select(
                           (row, rowIndex) => rowIndex + row.Aggregate(string.Empty, (s, i) => s + "," + GetValue(i))))
                   + Environment.NewLine;
        }

        private static string GetValue(int index)
        {
            return TestMatrix[index / TestMatrix.ColumnLength(), index % TestMatrix.ColumnLength()].ToString();
        }
    }
}