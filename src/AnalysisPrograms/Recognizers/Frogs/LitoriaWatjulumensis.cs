// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LitoriaWatjulumensis.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>
// <summary>
//
// </summary>
// --------------------------------------------------------------------------------------------------------------------

//using System.Linq;

namespace AnalysisPrograms.Recognizers.Frogs
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using Acoustics.Shared;
    using Acoustics.Shared.ConfigFile;
    using AnalysisBase;
    using AnalysisBase.ResultBases;
    using AnalysisPrograms.Recognizers.Base;
    using AudioAnalysisTools;
    using AudioAnalysisTools.DSP;
    using AudioAnalysisTools.Indices;
    using AudioAnalysisTools.StandardSpectrograms;
    using AudioAnalysisTools.WavTools;
    using log4net;

    //using Acoustics.Shared.Csv;
    using SixLabors.ImageSharp;
    using TowseyLibrary;

    /// <summary>
    /// To call this LitoriaWatjulumensis recognizer, the first command line argument must be "EventRecognizer".
    /// Alternatively, this recognizer can be called via the MultiRecognizer.
    ///
    /// This frog recognizer is based on the "kek-kek" recognizer for the Lewin's Rail
    /// It looks for synchronous oscillations in two frequency bands
    /// This recognizer has also been used for Litoria bicolor
    /// However the Correlation technique used for the Lewins Rail did not work because the oscilations in the upper and lower freq bands are not correlated.
    /// Instead measure the oscillations in the upper and lower bands independently.
    /// </summary>

    public class LitoriaWatjulumensis : RecognizerBase
    {
        public override string Description => "[ALPHA/EMBRYONIC] Detects acoustic events of Litoria watchamacallit.";

        public override string Author => "Towsey";

        public override string SpeciesName => "LitoriaWatjulumensis";

        public string SpeciesInitials = "L.w";

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Summarize your results. This method is invoked exactly once per original file.
        /// </summary>
        public override void SummariseResults(
            AnalysisSettings settings,
            FileSegment inputFileSegment,
            EventBase[] events,
            SummaryIndexBase[] indices,
            SpectralIndexBase[] spectralIndices,
            AnalysisResult2[] results)
        {
            // No operation - do nothing. Feel free to add your own logic.
            base.SummariseResults(settings, inputFileSegment, events, indices, spectralIndices, results);
        }

        // OTHER CONSTANTS
        //private const string ImageViewer = @"C:\Windows\system32\mspaint.exe";

        /// <summary>
        /// Do your analysis. This method is called once per segment (typically one-minute segments).
        /// </summary>
        public override RecognizerResults Recognize(AudioRecording recording, Config configuration, TimeSpan segmentStartOffset, Lazy<IndexCalculateResult[]> getSpectralIndexes, DirectoryInfo outputDirectory, int? imageWidth)
        {
            var recognizerConfig = new LitoriaWatjulumConfig();
            recognizerConfig.ReadConfigFile(configuration);

            //int maxOscilRate = (int)Math.Ceiling(1 / lwConfig.MinPeriod);

            if (recording.WavReader.SampleRate != 22050)
            {
                throw new InvalidOperationException("Requires a 22050Hz file");
            }

            TimeSpan recordingDuration = recording.WavReader.Time;

            // this default framesize seems to work
            const int frameSize = 128;
            double windowOverlap = 0.0;

            // calculate the overlap instead
            //double windowOverlap = Oscillations2012.CalculateRequiredFrameOverlap(
            //    recording.SampleRate,
            //    frameSize,
            //    maxOscilRate);

            // i: MAKE SONOGRAM
            var sonoConfig = new SonogramConfig
            {
                SourceFName = recording.BaseName,

                //set default values - ignore those set by user
                WindowSize = frameSize,
                WindowOverlap = windowOverlap,

                // the default window is HAMMING
                //WindowFunction = WindowFunctions.HANNING.ToString(),
                //WindowFunction = WindowFunctions.NONE.ToString(),
                // if do not use noise reduction can get a more sensitive recogniser.
                //NoiseReductionType = NoiseReductionType.NONE,
                NoiseReductionType = SNR.KeyToNoiseReductionType("STANDARD"),
            };

            //#############################################################################################################################################
            //DO THE ANALYSIS
            var results = Analysis(recording, sonoConfig, recognizerConfig, MainEntry.InDEBUG, segmentStartOffset);

            //######################################################################

            if (results == null)
            {
                return null; //nothing to process
            }

            var sonogram = results.Item1;
            var hits = results.Item2;
            var scoreArray = results.Item3;
            var predictedEvents = results.Item4;
            var debugImage = results.Item5;

            // old way of creating a path:
            //var debugPath = outputDirectory.Combine(FilenameHelpers.AnalysisResultName(Path.GetFileNameWithoutExtension(recording.FileName), SpeciesName, "png", "DebugSpectrogram"));
            var debugPath = FilenameHelpers.AnalysisResultPath(outputDirectory, recording.BaseName, this.SpeciesName, "png", "DebugSpectrogram");
            debugImage.Save(debugPath);

            //#############################################################################################################################################

            // Prune events here if required i.e. remove those below threshold score if this not already done. See other recognizers.
            foreach (var ae in predictedEvents)
            {
                // add additional info
                ae.Name = recognizerConfig.AbbreviatedSpeciesName;
                ae.SpeciesName = recognizerConfig.SpeciesName;
                ae.SegmentDurationSeconds = recordingDuration.TotalSeconds;
                ae.SegmentStartSeconds = segmentStartOffset.TotalSeconds;
            }

            var plot = new Plot(this.DisplayName, scoreArray, recognizerConfig.EventThreshold);
            return new RecognizerResults()
            {
                Sonogram = sonogram,
                Hits = hits,
                Plots = plot.AsList(),
                Events = predictedEvents,
            };
        }

        /// <summary>
        /// ################ THE KEY ANALYSIS METHOD for TRILLS
        ///
        ///  See Anthony's ExempliGratia.Recognize() method in order to see how to use methods for config profiles.
        ///  </summary>
        private static Tuple<BaseSonogram, double[,], double[], List<AcousticEvent>, Image> Analysis(
            AudioRecording recording,
            SonogramConfig sonoConfig,
            LitoriaWatjulumConfig lwConfig,
            bool returnDebugImage,
            TimeSpan segmentStartOffset)
        {
            double intensityThreshold = lwConfig.IntensityThreshold;
            double minDuration = lwConfig.MinDurationOfTrill;  // seconds
            double maxDuration = lwConfig.MaxDurationOfTrill;  // seconds
            double minPeriod = lwConfig.MinPeriod;  // seconds
            double maxPeriod = lwConfig.MaxPeriod;  // seconds

            if (recording == null)
            {
                LoggedConsole.WriteLine("AudioRecording == null. Analysis not possible.");
                return null;
            }

            //i: MAKE SONOGRAM
            //TimeSpan tsRecordingtDuration = recording.Duration();
            int sr = recording.SampleRate;
            double freqBinWidth = sr / (double)sonoConfig.WindowSize;
            double framesPerSecond = freqBinWidth;

            // duration of DCT in seconds - want it to be about 3X or 4X the expected maximum period
            double dctDuration = 4 * maxPeriod;

            // duration of DCT in frames
            int dctLength = (int)Math.Round(framesPerSecond * dctDuration);

            // set up the cosine coefficients
            double[,] cosines = MFCCStuff.Cosines(dctLength, dctLength);

            int upperBandMinBin = (int)Math.Round(lwConfig.UpperBandMinHz / freqBinWidth) + 1;
            int upperBandMaxBin = (int)Math.Round(lwConfig.UpperBandMaxHz / freqBinWidth) + 1;
            int lowerBandMinBin = (int)Math.Round(lwConfig.LowerBandMinHz / freqBinWidth) + 1;
            int lowerBandMaxBin = (int)Math.Round(lwConfig.LowerBandMaxHz / freqBinWidth) + 1;

            BaseSonogram sonogram = new SpectrogramStandard(sonoConfig, recording.WavReader);
            int rowCount = sonogram.Data.GetLength(0);

            //int colCount = sonogram.Data.GetLength(1);

            double[] lowerArray = MatrixTools.GetRowAveragesOfSubmatrix(sonogram.Data, 0, lowerBandMinBin, rowCount - 1, lowerBandMaxBin);
            double[] upperArray = MatrixTools.GetRowAveragesOfSubmatrix(sonogram.Data, 0, upperBandMinBin, rowCount - 1, upperBandMaxBin);

            //lowerArray = DataTools.filterMovingAverage(lowerArray, 3);
            //upperArray = DataTools.filterMovingAverage(upperArray, 3);

            double[] amplitudeScores = DataTools.SumMinusDifference(lowerArray, upperArray);
            double[] differenceScores = DspFilters.SubtractBaseline(amplitudeScores, 7);

            // Could smooth here rather than above. Above seemed slightly better?
            //amplitudeScores = DataTools.filterMovingAverage(amplitudeScores, 7);
            //differenceScores = DataTools.filterMovingAverage(differenceScores, 7);

            //iii: CONVERT decibel sum-diff SCORES TO ACOUSTIC TRILL EVENTS
            var predictedTrillEvents = AcousticEvent.ConvertScoreArray2Events(
                amplitudeScores,
                lwConfig.LowerBandMinHz,
                lwConfig.UpperBandMaxHz,
                sonogram.FramesPerSecond,
                freqBinWidth,
                lwConfig.DecibelThreshold,
                minDuration,
                maxDuration,
                segmentStartOffset);

            for (int i = 0; i < differenceScores.Length; i++)
            {
                if (differenceScores[i] < 1.0)
                {
                    differenceScores[i] = 0.0;
                }
            }

            // LOOK FOR TRILL EVENTS
            // init the score array
            double[] scores = new double[rowCount];

            // var hits = new double[rowCount, colCount];
            double[,] hits = null;

            // init confirmed events
            var confirmedEvents = new List<AcousticEvent>();

            // add names into the returned events
            foreach (var ae in predictedTrillEvents)
            {
                int eventStart = ae.Oblong.RowTop;
                int eventWidth = ae.Oblong.RowWidth;
                int step = 2;
                double maximumIntensity = 0.0;

                // scan the event to get oscillation period and intensity
                for (int i = eventStart - (dctLength / 2); i < eventStart + eventWidth - (dctLength / 2); i += step)
                {
                    // Look for oscillations in the difference array
                    double[] differenceArray = DataTools.Subarray(differenceScores, i, dctLength);
                    Oscillations2014.GetOscillationUsingDct(differenceArray, framesPerSecond, cosines, out var oscilFreq, out var period, out var intensity);

                    bool periodWithinBounds = period > minPeriod && period < maxPeriod;

                    //Console.WriteLine($"step={i}    period={period:f4}");

                    if (!periodWithinBounds)
                    {
                        continue;
                    }

                    // lay down score for sample length
                    for (int j = 0; j < dctLength; j++)
                    {
                        if (scores[i + j] < intensity)
                        {
                            scores[i + j] = intensity;
                        }
                    }

                    if (maximumIntensity < intensity)
                    {
                        maximumIntensity = intensity;
                    }
                }

                // add abbreviatedSpeciesName into event
                if (maximumIntensity >= intensityThreshold)
                {
                    ae.Name = $"{lwConfig.AbbreviatedSpeciesName}.{lwConfig.ProfileNames[0]}";
                    ae.Score_MaxInEvent = maximumIntensity;
                    ae.Profile = lwConfig.ProfileNames[0];
                    confirmedEvents.Add(ae);
                }
            }

            //######################################################################
            // LOOK FOR TINK EVENTS
            // CONVERT decibel sum-diff SCORES TO ACOUSTIC EVENTS
            double minDurationOfTink = lwConfig.MinDurationOfTink;  // seconds
            double maxDurationOfTink = lwConfig.MaxDurationOfTink;  // seconds

            // want stronger threshold for tink because brief.
            double tinkDecibelThreshold = lwConfig.DecibelThreshold + 3.0;
            var predictedTinkEvents = AcousticEvent.ConvertScoreArray2Events(
                amplitudeScores,
                lwConfig.LowerBandMinHz,
                lwConfig.UpperBandMaxHz,
                sonogram.FramesPerSecond,
                freqBinWidth,
                tinkDecibelThreshold,
                minDurationOfTink,
                maxDurationOfTink,
                segmentStartOffset);
            foreach (var ae2 in predictedTinkEvents)
            {
                // Prune the list of potential acoustic events, for example using Cosine Similarity.

                //rowtop,  rowWidth
                //int eventStart = ae2.Oblong.RowTop;
                //int eventWidth = ae2.Oblong.RowWidth;
                //int step = 2;
                //double maximumIntensity = 0.0;

                // add abbreviatedSpeciesName into event
                //if (maximumIntensity >= intensityThreshold)
                //{
                ae2.Name = $"{lwConfig.AbbreviatedSpeciesName}.{lwConfig.ProfileNames[1]}";

                //ae2.Score_MaxInEvent = maximumIntensity;
                ae2.Profile = lwConfig.ProfileNames[1];
                confirmedEvents.Add(ae2);

                //}
            }

            //######################################################################

            var scorePlot = new Plot(lwConfig.SpeciesName, scores, intensityThreshold);
            Image debugImage = null;

            if (returnDebugImage)
            {
                // display a variety of debug score arrays
                DataTools.Normalise(amplitudeScores, lwConfig.DecibelThreshold, out var normalisedScores, out var normalisedThreshold);
                var sumDiffPlot = new Plot("Sum Minus Difference", normalisedScores, normalisedThreshold);
                DataTools.Normalise(differenceScores, lwConfig.DecibelThreshold, out normalisedScores, out normalisedThreshold);
                var differencePlot = new Plot("Baseline Removed", normalisedScores, normalisedThreshold);

                var debugPlots = new List<Plot> { scorePlot, sumDiffPlot, differencePlot };
                debugImage = DrawDebugImage(sonogram, confirmedEvents, debugPlots, hits);
            }

            // return new sonogram because it makes for more easy interpretation of the image
            var returnSonoConfig = new SonogramConfig
            {
                SourceFName = recording.BaseName,
                WindowSize = 512,
                WindowOverlap = 0,

                // the default window is HAMMING
                //WindowFunction = WindowFunctions.HANNING.ToString(),
                //WindowFunction = WindowFunctions.NONE.ToString(),
                // if do not use noise reduction can get a more sensitive recogniser.
                //NoiseReductionType = NoiseReductionType.NONE,
                NoiseReductionType = SNR.KeyToNoiseReductionType("STANDARD"),
            };
            BaseSonogram returnSonogram = new SpectrogramStandard(returnSonoConfig, recording.WavReader);
            return Tuple.Create(returnSonogram, hits, scores, confirmedEvents, debugImage);
        } //Analysis()
    } //end class LitoriaWatjulumensis.

    /// <summary>
    /// See Anthony's ExempliGratia.Recognize() method in order to see how to use methods for config profiles.
    /// </summary>
    internal class LitoriaWatjulumConfig
    {
        public string AnalysisName { get; set; }

        public string SpeciesName { get; set; }

        public string AbbreviatedSpeciesName { get; set; }

        public int UpperBandMinHz { get; set; }

        public int UpperBandMaxHz { get; set; }

        public int LowerBandMinHz { get; set; }

        public int LowerBandMaxHz { get; set; }

        public double MinPeriod { get; set; }

        public double MaxPeriod { get; set; }

        public double IntensityThreshold { get; set; }

        public double DecibelThreshold { get; set; }

        public double MinDurationOfTrill { get; set; }

        public double MaxDurationOfTrill { get; set; }

        public double EventThreshold { get; set; }

        public double MinDurationOfTink { get; set; }

        public double MaxDurationOfTink { get; set; }

        public string[] ProfileNames { get; set; }

        internal void ReadConfigFile(Config configuration)
        {
            // common properties
            this.AnalysisName = configuration[AnalysisKeys.AnalysisName] ?? "<no name>";
            this.SpeciesName = configuration[AnalysisKeys.SpeciesName] ?? "<no species>";
            this.AbbreviatedSpeciesName = configuration[AnalysisKeys.AbbreviatedSpeciesName] ?? "<no.sp>";
            this.UpperBandMaxHz = configuration.GetInt("UpperFreqBandTop");
            this.UpperBandMinHz = configuration.GetInt("UpperFreqBandBottom");
            this.LowerBandMaxHz = configuration.GetInt("LowerFreqBandTop");
            this.LowerBandMinHz = configuration.GetInt("LowerFreqBandBottom");
            this.DecibelThreshold = configuration.GetDoubleOrNull(AnalysisKeys.DecibelThreshold) ?? 3.0;

            // extract profiles
            bool hasProfiles = ConfigFile.HasProfiles(configuration);
            if (!hasProfiles)
            {
                throw new ConfigFileException($"The Config file for {this.SpeciesName} must contain at least one valid profile.");
            }

            this.ProfileNames = ConfigFile.GetProfileNames(configuration);
            foreach (var name in this.ProfileNames)
            {
                LoggedConsole.WriteLine($"The Config file for {this.SpeciesName}  contains the profile <{name}>.");
            }

            // Config profile = ConfigFile.GetProfile(configuration, "Trill");
            bool success = ConfigFile.TryGetProfile(configuration, "Trill", out var trillProfile);
            if (!success)
            {
                throw new ConfigFileException($"The Config file for {this.SpeciesName} must contain a \"Trill\" profile.");
            }

            LoggedConsole.WriteLine($"Analyzing profile: {this.ProfileNames[0]}");

            // Periods and Oscillations
            this.MinPeriod = trillProfile.GetDouble(AnalysisKeys.MinPeriodicity); //: 0.18
            this.MaxPeriod = trillProfile.GetDouble(AnalysisKeys.MaxPeriodicity); //: 0.25

            // minimum duration in seconds of a trill event
            this.MinDurationOfTrill = trillProfile.GetDouble(AnalysisKeys.MinDuration); //:3

            // maximum duration in seconds of a trill event
            this.MaxDurationOfTrill = trillProfile.GetDouble(AnalysisKeys.MaxDuration); //: 15
            //// minimum acceptable value of a DCT coefficient
            this.IntensityThreshold = trillProfile.GetDoubleOrNull(AnalysisKeys.IntensityThreshold) ?? 0.4;

            //double dctDuration = (double)configuration.GetDouble(AnalysisKeys.DctDuration);
            // This is the intensity threshold above
            //double dctThreshold = (double)configuration.GetDouble(AnalysisKeys.DctThreshold);

            LoggedConsole.WriteLine($"Analyzing profile: {this.ProfileNames[1]}");
            success = ConfigFile.TryGetProfile(configuration, "Tink", out var tinkProfile);
            if (!success)
            {
                throw new ConfigFileException($"The Config file for {this.SpeciesName} must contain a \"Tink\" profile.");
            }

            this.MinDurationOfTink = tinkProfile.GetDouble(AnalysisKeys.MinDuration);

            // maximum duration in seconds of a tink event
            this.MaxDurationOfTink = tinkProfile.GetDouble(AnalysisKeys.MaxDuration);
            this.EventThreshold = trillProfile.GetDoubleOrNull(AnalysisKeys.EventThreshold) ?? 0.2;
        }
    } //class LitoriaWatjulumConfig
}