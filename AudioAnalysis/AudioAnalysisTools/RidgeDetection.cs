﻿using AudioAnalysisTools.StandardSpectrograms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TowseyLibrary;


namespace AudioAnalysisTools
{
    public class RidgeDetection
    {
        public static double ridgeDetectionmMagnitudeThreshold = 0.2;
        public static int ridgeMatrixLength = 5;
        public static int filterRidgeMatrixLength = 5;
        public static int minimumNumberInRidgeInMatrix = 6;


        public class RidgeDetectionConfiguration
        {
            public double RidgeDetectionmMagnitudeThreshold { get; set; }

            /// <summary>
            /// dimension of NxN matrix to use for ridge detection, must be odd number.
            /// </summary>
            public int RidgeMatrixLength { get; set; }

            public int FilterRidgeMatrixLength { get; set; }

            public int MinimumNumberInRidgeInMatrix { get; set; }
        }



        //public static List<PointOfInterest> PostRidgeDetection(SpectrogramStandard spectrogram, RidgeDetectionConfiguration ridgeConfig)
        //{
        //    var instance = new POISelection(new List<PointOfInterest>());
        //    instance.FourDirectionsRidgeDetection(spectrogram, ridgeConfig);
        //    return instance.poiList;
        //}


        public static byte[,] Sobel5X5RidgeDetection(double[,] matrix, double magnitudeThreshold)
        {
            //int ridgeLength = ridgeConfiguration.RidgeMatrixLength;
            //double magnitudeThreshold = ridgeConfiguration.RidgeDetectionmMagnitudeThreshold;

            //double secondsScale = spectrogram.Configuration.GetFrameOffset(spectrogram.SampleRate); // 0.0116
            //var timeScale = TimeSpan.FromTicks((long)(TimeSpan.TicksPerSecond * secondsScale)); // Time scale here is millionSecond?
            //double herzScale = spectrogram.FBinWidth; //43 hz
            //double freqBinCount = spectrogram.Configuration.FreqBinCount; //256
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            int halfLength = 2;

            //A: CONVERT MATRIX to BINARY FORM INDICATING SPECTRAL RIDGES
            var hits = new byte[rows, cols];



            for (int r = halfLength; r < rows - halfLength; r++)
            {
                for (int c = halfLength; c < cols - halfLength; c++)
                {
                    var subM = MatrixTools.Submatrix(matrix, r - halfLength, c - halfLength, r + halfLength, c + halfLength); // extract NxN submatrix
                    double magnitude;
                    // direction is multiple of pi/4, i.e. 0. pi/4, pi/2, 3pi/4. 
                    double direction;
                    bool isRidge = false;

                    // magnitude is dB
                    ImageTools.Sobel5X5RidgeDetection(subM, out isRidge, out magnitude, out direction);
                    if ((magnitude > magnitudeThreshold) && (isRidge == true))
                    {
                        // Ridge orientation Category only has four values, they are 0, 1, 2, 3. 
                        //int orientationCategory = (int)Math.Round((direction * 8) / Math.PI);
                        hits[r, c] = (byte)(direction + 1);
                    }
                }
            }  
            
            /// filter out some redundant ridges
            //var prunedPoiList = ImageTools.PruneAdjacentTracks(poiList, rows, cols);
            //var prunedPoiList1 = ImageTools.IntraPruneAdjacentTracks(prunedPoiList, rows, cols);
            ////var filteredPoiList = ImageAnalysisTools.RemoveIsolatedPoi(prunedPoiList1, rows, cols, ridgeConfiguration.FilterRidgeMatrixLength, ridgeConfiguration.MinimumNumberInRidgeInMatrix);
            //var filteredPoiList = ImageTools.FilterRidges(prunedPoiList1, rows, cols, ridgeConfiguration.FilterRidgeMatrixLength, ridgeConfiguration.MinimumNumberInRidgeInMatrix);
            return hits;
        }


        // ############################################################################################################################
        // METHODS BELOW HERE ARE OLDER AND TRANSFERED FROM THE MATRIXTOOLS class in September 2014. 
        // ############################################################################################################################

        /// <summary>
        /// 
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static byte[,] IdentifySpectralRidges(double[,] matrix, double threshold)
        {
            var binary1 = IdentifyHorizontalRidges(matrix, threshold);
            //binary1 = JoinDisconnectedRidgesInBinaryMatrix(binary1, matrix, threshold);

            var m2 = DataTools.MatrixTranspose(matrix);
            var binary2 = IdentifyHorizontalRidges(m2, threshold);
            //binary2 = JoinDisconnectedRidgesInBinaryMatrix(binary2, m2, threshold);
            binary2 = DataTools.MatrixTranspose(binary2);
            //ImageTools.Sobel5X5RidgeDetection();

            //merge the two binary matrices
            int rows = binary1.GetLength(0);
            int cols = binary1.GetLength(1);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    if (binary2[r, c] == 1) binary1[r, c] = 1;
                }

            return binary1;
        }

        public static byte[,] IdentifySpectralRidgesInFreqDirection(double[,] matrix, double threshold)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            //A: CONVERT MATRIX to BINARY FORM INDICATING SPECTRAL RIDGES
            var binary = new byte[rows, cols];
            for (int r = 0; r < rows; r++) //row at a time, each row = one frame.
            {
                double[] row = DataTools.GetRow(matrix, r);
                row = DataTools.filterMovingAverage(row, 3); //## SMOOTH FREQ BIN - high value breaks up vertical tracks
                for (int c = 3; c < cols - 3; c++)
                {
                    double d1 = row[c] - row[c - 1];
                    double d2 = row[c] - row[c + 1];
                    double d3 = row[c] - row[c - 2];
                    double d4 = row[c] - row[c + 2];
                    double d5 = row[c] - row[c - 3];
                    double d6 = row[c] - row[c + 3];
                    //identify a peak
                    if ((d1 > threshold) && (d2 > threshold) && (d3 > threshold) && (d4 > threshold) && (d5 > threshold) && (d6 > threshold))
                    {
                        binary[r, c] = 1;
                    }
                } //end for every col
            } //end for every row
            return binary;
        }

        public static byte[,] IdentifyHorizontalRidges(double[,] matrix, double threshold)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            //A: CONVERT MATRIX to BINARY FORM INDICATING SPECTRAL RIDGES
            var binary = new byte[rows, cols];
            for (int r = 2; r < rows - 2; r++) //row at a time, each row = one frame.
            {
                for (int c = 2; c < cols - 2; c++)
                {
                    //identify a peak
                    double sumTop2 = matrix[r - 2, c - 2] + matrix[r - 2, c - 1] + matrix[r - 2, c] + matrix[r - 2, c + 1] + matrix[r - 2, c + 2];
                    double sumTop1 = matrix[r - 1, c - 2] + matrix[r - 1, c - 1] + matrix[r - 1, c] + matrix[r - 1, c + 1] + matrix[r - 1, c + 2];
                    double sumMid = matrix[r, c - 2] + matrix[r, c - 1] + matrix[r, c] + matrix[r, c + 1] + matrix[r, c + 2];
                    double sumBtm1 = matrix[r + 1, c - 2] + matrix[r + 1, c - 1] + matrix[r + 1, c] + matrix[r + 1, c + 1] + matrix[r + 1, c + 2];
                    double sumBtm2 = matrix[r + 2, c - 2] + matrix[r + 2, c - 1] + matrix[r + 2, c] + matrix[r + 2, c + 1] + matrix[r + 2, c + 2];
                    double avTop = (sumTop2 + sumTop1) / (double)10;
                    double avBtm = (sumBtm2 + sumBtm1) / (double)10;
                    double avMdl = sumMid / (double)5;
                    double dTop = avMdl - avTop;
                    double dBtm = avMdl - avBtm;

                    if ((dTop > threshold) && (dBtm > threshold))
                    {
                        binary[r, c - 2] = 1;
                        binary[r, c - 1] = 1;
                        binary[r, c] = 1;
                        binary[r, c + 1] = 1;
                        binary[r, c + 2] = 1;
                    }

                } //end for every col
            } //end for every row
            return binary;
        }


        /// <summary>
        ///JOINS DISCONNECTED RIDGES
        /// </summary>
        /// <returns></returns>
        public static byte[,] JoinDisconnectedRidgesInBinaryMatrix(byte[,] binary, double[,] matrix, double threshold)
        {
            int rows = binary.GetLength(0);
            int cols = binary.GetLength(1);
            byte[,] newM = new byte[rows, cols];

            for (int r = 0; r < rows - 3; r++) //row at a time, each row = one frame.
            {
                for (int c = 3; c < cols - 3; c++)
                {
                    if (binary[r, c] == 0) continue; //no peak to join
                    if (matrix[r, c] < threshold)
                    {
                        binary[r, c] = 0;
                        continue; // peak too weak to join
                    }

                    newM[r, c] = 1; // pixel r,c = 1.0
                    // skip if adjacent pixels in next row also = 1.0
                    if (binary[r + 1, c] == 1) continue;
                    if (binary[r + 1, c - 1] == 1) continue;
                    if (binary[r + 1, c + 1] == 1) continue;

                    // fill in the same column
                    if ((binary[r + 3, c] == 1.0)) newM[r + 2, c] = 1; //fill gap
                    if ((binary[r + 2, c] == 1.0)) newM[r + 1, c] = 1; //fill gap

                    if ((binary[r + 2, c - 3] == 1.0)) newM[r + 1, c - 2] = 1; //fill gap
                    if ((binary[r + 2, c + 3] == 1.0)) newM[r + 1, c + 2] = 1; //fill gap


                    //if ((binary[r + 2, c - 2] == 1.0)) newM[r + 1, c - 1] = 1; //fill gap
                    //if ((binary[r + 2, c + 2] == 1.0)) newM[r + 1, c + 1] = 1; //fill gap

                    if ((binary[r + 1, c - 2] == 1.0)) newM[r + 1, c - 1] = 1; //fill gap
                    if ((binary[r + 1, c + 2] == 1.0)) newM[r + 1, c + 1] = 1; //fill gap
                }
            }
            return newM;
        }

        public static byte[,] JoinDisconnectedRidgesInBinaryMatrix1(byte[,] binary)
        {
            int rows = binary.GetLength(0);
            int cols = binary.GetLength(1);
            byte[,] newM = new byte[rows, cols];

            for (int r = 0; r < rows - 3; r++) //row at a time, each row = one frame.
            {
                for (int c = 3; c < cols - 3; c++)
                {
                    if (binary[r, c] == 0.0) continue;

                    newM[r, c] = 1;
                    // pixel r,c = 1.0 - skip if adjacent pixels in next row also = 1.0
                    if (binary[r + 1, c] == 1) continue;
                    if (binary[r + 1, c - 1] == 1) continue;
                    if (binary[r + 1, c + 1] == 1) continue;

                    //fill in the same column
                    if ((binary[r + 3, c] == 1.0)) newM[r + 2, c] = 1; //fill gap
                    if ((binary[r + 2, c] == 1.0)) newM[r + 1, c] = 1; //fill gap

                    if ((binary[r + 2, c - 3] == 1.0)) newM[r + 1, c - 2] = 1; //fill gap
                    if ((binary[r + 2, c + 3] == 1.0)) newM[r + 1, c + 2] = 1; //fill gap


                    //if ((binary[r + 2, c - 2] == 1.0)) newM[r + 1, c - 1] = 1; //fill gap
                    //if ((binary[r + 2, c + 2] == 1.0)) newM[r + 1, c + 1] = 1; //fill gap

                    if ((binary[r + 1, c - 2] == 1.0)) newM[r + 1, c - 1] = 1; //fill gap
                    if ((binary[r + 1, c + 2] == 1.0)) newM[r + 1, c + 1] = 1; //fill gap
                }
            }
            return newM;
        }


        /// <summary>
        /// CONVERTs a binary matrix of spectral peak tracks to an output matrix containing the acoustic intensity
        /// in the neighbourhood of those peak tracks.
        /// </summary>
        /// <param name="binary">The spectral peak tracks</param>
        /// <param name="matrix">The original sonogram</param>
        /// <returns></returns>
        public static double[,] SpectralRidges2Intensity(byte[,] binary, double[,] sonogram)
        {
            //speak track neighbourhood
            int rNH = 5;
            int cNH = 1;

            double minIntensity; // min value in matrix
            double maxIntensity; // max value in matrix
            DataTools.MinMax(sonogram, out minIntensity, out maxIntensity);

            int rows = sonogram.GetLength(0);
            int cols = sonogram.GetLength(1);
            double[,] outM = new double[rows, cols];
            //initialise the output matrix/sonogram to the minimum acoustic intensity
            for (int r = 0; r < rows; r++) //init matrix to min
            {
                for (int c = 0; c < cols; c++) outM[r, c] = minIntensity; //init output matrix to min value
            }

            double localdb;
            for (int r = rNH; r < rows - rNH; r++) //row at a time, each row = one frame.
            {
                for (int c = cNH; c < cols - cNH; c++)
                {
                    if (binary[r, c] == 0.0) continue;

                    localdb = sonogram[r, c] - 3.0; //local lower bound = twice min perceptible difference
                    //scan neighbourhood
                    for (int i = r - rNH; i <= (r + rNH); i++)
                    {
                        for (int j = c - cNH; j <= (c + cNH); j++)
                        {
                            if (sonogram[i, j] > localdb) outM[i, j] = sonogram[i, j];
                            if (outM[i, j] < minIntensity) outM[i, j] = minIntensity;
                        }
                    } //end local NH
                }
            }
            return outM;
        }



        public static double[,] IdentifySpectralPeaks(double[,] matrix)
        {
            double buffer = 3.0; //dB peak requirement
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            //A: CONVERT MATRIX to BINARY FORM INDICATING SPECTRAL PEAKS
            double[,] binary = new double[rows, cols];
            for (int r = 2; r < rows - 2; r++) //row at a time, each row = one frame.
            {
                for (int c = 2; c < cols - 2; c++)
                {
                    //identify a peak
                    if ((matrix[r, c] > matrix[r, c - 2] + buffer) && (matrix[r, c] > matrix[r, c + 2] + buffer)
                        //same row
                        && (matrix[r, c] > matrix[r - 2, c] + buffer) && (matrix[r, c] > matrix[r + 2, c] + buffer)
                        //same col
                        && (matrix[r, c] > matrix[r - 1, c - 1] + buffer)
                        && (matrix[r, c] > matrix[r + 1, c + 1] + buffer) //diagonal
                        && (matrix[r, c] > matrix[r - 1, c + 1] + buffer)
                        && (matrix[r, c] > matrix[r + 1, c - 1] + buffer)) //other diag
                    {
                        binary[r, c] = 1.0; // maxIntensity;
                        binary[r - 1, c - 1] = 1.0; // maxIntensity;
                        binary[r + 1, c + 1] = 1.0; // maxIntensity;
                        binary[r - 1, c + 1] = 1.0; // maxIntensity;
                        binary[r + 1, c - 1] = 1.0; // maxIntensity;
                        binary[r, c - 1] = 1.0; // maxIntensity;
                        binary[r, c + 1] = 1.0; // maxIntensity;
                        binary[r - 1, c] = 1.0; // maxIntensity;
                        binary[r + 1, c] = 1.0; // maxIntensity;
                    }
                    //else binary[r, c] = 0.0; // minIntensity;
                } //end for every col
                //binary[r, 0] = 0; // minIntensity;
                //binary[r, 1] = 0; // minIntensity;
                //binary[r, cols - 2] = 0; //minIntensity;
                //binary[r, cols - 1] = 0; //minIntensity;
            } //end for every row

            return binary;
        }

    
    }

}