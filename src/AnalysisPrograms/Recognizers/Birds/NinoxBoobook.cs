// <copyright file="NinoxBoobook.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace AnalysisPrograms.Recognizers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Acoustics.Shared.ConfigFile;
    using AnalysisBase;
    using AnalysisPrograms.Recognizers.Base;
    using AudioAnalysisTools;
    using AudioAnalysisTools.DSP;
    using AudioAnalysisTools.Events;
    using AudioAnalysisTools.Events.Types;
    using AudioAnalysisTools.Indices;
    using AudioAnalysisTools.StandardSpectrograms;
    using AudioAnalysisTools.WavTools;
    using log4net;
    using SixLabors.ImageSharp;
    using TowseyLibrary;
    using static AnalysisPrograms.Recognizers.GenericRecognizer;
    using Path = System.IO.Path;

    /// <summary>
    /// A recognizer for the Australian Boobook Owl, /// https://en.wikipedia.org/wiki/Australian_boobook .
    /// Eight subspecies of the Australian boobook are recognized,
    /// with three further subspecies being reclassified as separate species in 2019 due to their distinctive calls and genetics.
    /// THis recognizer has been trained on good quality calls from the Gympie recordings obtained by Yvonne Phillips.
    /// The recognizer has also been run across several recordings of Boobook from NZ (recordings obtained from Stuart Parsons.
    /// The NZ Boobook calls were of poor quality (distant and echo) and were 200 Hertz higher and performance was not good.
    /// </summary>
    internal class NinoxBoobook : RecognizerBase
    {
        private static readonly ILog BoobookLog = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public override string Author => "Towsey";

        public override string SpeciesName => "NinoxBoobook";

        public override string Description => "[ALPHA] Detects acoustic events for the Australian Boobook owl.";

        public override AnalyzerConfig ParseConfig(FileInfo file)
        {
            RuntimeHelpers.RunClassConstructor(typeof(NinoxBoobookConfig).TypeHandle);
            var config = ConfigFile.Deserialize<NinoxBoobookConfig>(file);

            // validation of configs can be done here
            GenericRecognizer.ValidateProfileTagsMatchAlgorithms(config.Profiles, file);

            // This call sets a restriction so that only one generic algorithm is used.
            // CHANGE this to accept multiple generic algorithms as required.
            //if (result.Profiles.SingleOrDefault() is ForwardTrackParameters)
            if (config.Profiles?.Count == 1 && config.Profiles.First().Value is ForwardTrackParameters)
            {
                return config;
            }

            throw new ConfigFileException("NinoxBoobook expects one and only one ForwardTrack algorithm.", file);
        }

        /// <summary>
        /// This method is called once per segment (typically one-minute segments).
        /// </summary>
        /// <param name="audioRecording">one minute of audio recording.</param>
        /// <param name="config">config file that contains parameters used by all profiles.</param>
        /// <param name="segmentStartOffset">when recording starts.</param>
        /// <param name="getSpectralIndexes">not sure what this is.</param>
        /// <param name="outputDirectory">where the recognizer results can be found.</param>
        /// <param name="imageWidth"> assuming ????.</param>
        /// <returns>recognizer results.</returns>
        public override RecognizerResults Recognize(
            AudioRecording audioRecording,
            Config config,
            TimeSpan segmentStartOffset,
            Lazy<IndexCalculateResult[]> getSpectralIndexes,
            DirectoryInfo outputDirectory,
            int? imageWidth)
        {
            //class NinoxBoobookConfig is define at bottom of this file.
            var genericConfig = (NinoxBoobookConfig)config;
            var recognizer = new GenericRecognizer();

            RecognizerResults combinedResults = recognizer.Recognize(
                audioRecording,
                genericConfig,
                segmentStartOffset,
                getSpectralIndexes,
                outputDirectory,
                imageWidth);

            // DO POST-PROCESSING of EVENTS

            // Convert events to spectral events for possible combining.
            // Combine overlapping events. If the dB threshold is set low, may get lots of little events.
            var events = combinedResults.NewEvents;
            var spectralEvents = events.Select(x => (SpectralEvent)x).ToList();
            var newEvents = CompositeEvent.CombineOverlappingEvents(spectralEvents);

            if (genericConfig.CombinePossibleSyllableSequence)
            {
                // convert events to spectral events for possible combining.
                //var spectralEvents = events.Select(x => (SpectralEvent)x).ToList();
                spectralEvents = newEvents.Cast<SpectralEvent>().ToList();
                var startDiff = genericConfig.SyllableStartDifference;
                var hertzDiff = genericConfig.SyllableHertzGap;
                newEvents = CompositeEvent.CombineSimilarProximalEvents(spectralEvents, TimeSpan.FromSeconds(startDiff), (int)hertzDiff);
            }

            combinedResults.NewEvents = newEvents;

            //UNCOMMENT following line if you want special debug spectrogram, i.e. with special plots.
            //  NOTE: Standard spectrograms are produced by setting SaveSonogramImages: "True" or "WhenEventsDetected" in <Towsey.PteropusSpecies.yml> config file.
            //GenericRecognizer.SaveDebugSpectrogram(territorialResults, genericConfig, outputDirectory, audioRecording.BaseName);
            return combinedResults;
        }

        /*
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
        */

        /// <inheritdoc cref="NinoxBoobookConfig"/> />
        public class NinoxBoobookConfig : GenericRecognizerConfig, INamedProfiles<object>
        {
            public bool CombinePossibleSyllableSequence { get; set; } = false;

            public double SyllableStartDifference { get; set; } = 0.5;

            public double SyllableHertzGap { get; set; } = 200;
        }
    }
}