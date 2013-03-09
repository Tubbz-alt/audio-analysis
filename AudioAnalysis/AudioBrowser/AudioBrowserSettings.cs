﻿namespace AudioBrowser
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Windows.Forms;

    using Acoustics.Shared;
    using System.Text;

    public class AudioBrowserSettings
    {
        //OBLIGATORY KEYS REQUIRED FOR ALL ANALYSES - these are used by the browser to run an analysis
        public const string key_SEGMENT_DURATION = "SEGMENT_DURATION";
        public const string key_SEGMENT_OVERLAP  = "SEGMENT_OVERLAP";
        public const string key_FRAME_LENGTH     = "FRAME_LENGTH";
        public const string key_FRAME_OVERLAP    = "FRAME_OVERLAP";
        public const string key_RESAMPLE_RATE    = "RESAMPLE_RATE";

        public const string DefaultConfigExt = ".cfg";

        /// <summary>
        /// loads settings from the executable's config file.
        /// If directories given in config file do not exist then create a temp directory in C drive.
        /// </summary>
        public void LoadBrowserSettings()
        {
            char tick = '\u2714';
            char cross = '\u2718';

            LoggedConsole.WriteLine("\n####################################################################################\nLOADING BROWSER SETTINGS:");

            this.DefaultTempFilesDir = AppConfigHelper.GetDir("TempFileDirectory", false);

            try
            {
                this.DefaultConfigDir = AppConfigHelper.GetDir("DefaultConfigDir", true);
                this.diConfigDir = this.DefaultConfigDir;
                LoggedConsole.WriteLine(tick + " Found the default config directory.");
            }
            catch (DirectoryNotFoundException ex)
            {
                LoggedConsole.WriteLine("WARNING!  The default directory containing analysis config files was not found. \n" + ex.ToString());
                LoggedConsole.WriteLine("          You will not be able to analyse audio files.");
                LoggedConsole.WriteLine("          Enter correct directory location of the config files in the app.config file.");

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
            } //catch

            try
            {
                // specify false rather than true so GetDir doesn't throw exceptions
                this.DefaultSourceDir = AppConfigHelper.GetDir("DefaultSourceDir", true);
                this.diSourceDir = this.DefaultSourceDir;
                LoggedConsole.WriteLine(tick + " Found the default source audio directory.");
            }
            catch (DirectoryNotFoundException ex)
            {
                string lastResort = @"C:\temp";
                this.diSourceDir = new DirectoryInfo(lastResort);
                if (!diSourceDir.Exists) diSourceDir.Create();

                LoggedConsole.WriteLine("WARNING!  The default source directory was not found. <" + this.DefaultSourceDir + ">");
                LoggedConsole.WriteLine("          Created new directory <" + this.diSourceDir.FullName + "> \n\n");
            } //catch

            try
            {
                this.DefaultOutputDir = AppConfigHelper.GetDir("DefaultOutputDir", false);
                if (!this.DefaultOutputDir.Exists) 
                {
                    this.DefaultOutputDir = this.DefaultOutputDir.Parent;
                    if (!this.DefaultOutputDir.Exists)
                    {
                        this.DefaultOutputDir = this.DefaultOutputDir.Parent;
                        if (!this.DefaultOutputDir.Exists) throw new DirectoryNotFoundException();
                    }
                }
                this.diOutputDir = this.DefaultOutputDir;
                LoggedConsole.WriteLine(tick + " Found the default output directory.");
            }
            catch (DirectoryNotFoundException ex)
            {
                string lastResort = @"C:\temp";
                this.diOutputDir = new DirectoryInfo(lastResort);
                if (!diOutputDir.Exists) diOutputDir.Create();

                LoggedConsole.WriteLine("WARNING!  The default output directory was not found. <" + this.DefaultOutputDir + ">");
                LoggedConsole.WriteLine("          Created new directory <" + this.diOutputDir.FullName + "> \n\n");

            } //catch

            // check for remainder of app.config arguments
            try
            {
                //this.AnalysisList = AppConfigHelper.GetStrings("AnalysisList", ',');
                this.AnalysisIdentifier = AppConfigHelper.GetString("DefaultAnalysisName");
                this.fiAnalysisConfig = new FileInfo(Path.Combine(diConfigDir.FullName, AnalysisIdentifier + DefaultConfigExt));

                this.DefaultSegmentDuration = AppConfigHelper.GetDouble("DefaultSegmentDuration");
                this.DefaultResampleRate = AppConfigHelper.GetInt("DefaultResampleRate");
                this.SourceFileExt = AppConfigHelper.GetString("SourceFileExt");
                this.SonogramBackgroundThreshold = AppConfigHelper.GetDouble("SonogramBackgroundThreshold");
                this.TrackHeight = AppConfigHelper.GetInt("TrackHeight");
                this.TrackCount = AppConfigHelper.GetInt("TrackCount");
                this.TrackNormalisedDisplay = AppConfigHelper.GetBool("TrackNormalisedDisplay");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                MessageBox.Show("WARNING: COULD NOT READ ALL ITEMS FROM THE APP.CONFIG. CHECK contents of app.config in working directory.");
                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
            } //catch


            //CHECK THAT AUDIO SOX.exe and other AUDIO ANALYSIS FILES EXIST
            if (AudioAnalysisFilesExist())
            {
                LoggedConsole.WriteLine(tick + " Located SOX and other audio analysis files");
            }
            else
            {
                // MessageBox.Show("WARNING: " + ex.ToString());
                // MessageBox.Show("  CHECK paths in app.config file for following executable files: Ffmpeg.exe, Ffprobe.exe, Wvunpack.exe, Mp3Splt.exe, Sox.exe");
                LoggedConsole.WriteLine(cross + " WARNING!  Could not find one or more of the following audio analysis files:");
                LoggedConsole.WriteLine("          Ffmpeg.exe, Ffprobe.exe, Wvunpack.exe, Mp3Splt.exe, Sox.exe");
                LoggedConsole.WriteLine("          You will not be able to work with the original source file.");

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }
            }

            if (AudacityExists())
            {
                LoggedConsole.WriteLine(tick + " Audacity located");
            }
            else
            {
                //MessageBox.Show("WARNING: Unable to find Audacity. Enter correct location in the app.config file.");
                LoggedConsole.WriteLine("WARNING!  Unable to find Audacity at default locations.");
                LoggedConsole.WriteLine("          Audacity.exe is required to view spectrograms of source recording.");
                LoggedConsole.WriteLine("          Enter correct location in the app.config file.");
            }

            if (WordPadExists())
            {
                LoggedConsole.WriteLine(tick + " WordPad located");
            }
            else
            {
                LoggedConsole.WriteLine("WARNING!  Unable to find WordPad at default location.");
                LoggedConsole.WriteLine("          A text editor may be required to edit the analysis config files.");
                LoggedConsole.WriteLine("          Enter correct location in the app.config file.");
            }

        } // LoadBrowserSettings()




        public void WriteSettings2Console()
        {
            LoggedConsole.WriteLine();
            LoggedConsole.WriteLine("# Browser Settings:");
            LoggedConsole.WriteLine("\tAnalysis Name: " + this.AnalysisIdentifier);
            if (this.fiAnalysisConfig == null)
            {
                LoggedConsole.WriteLine("\tAnalysis Config File: NULL");
            }
            else
            {
                LoggedConsole.WriteLine("\tAnalysis Config File: " + this.fiAnalysisConfig.FullName);
            }
            LoggedConsole.WriteLine("\tSource Directory:     " + this.diSourceDir.FullName);
            LoggedConsole.WriteLine("\tOutput Directory:     " + this.diOutputDir.FullName);
            if (AudacityExists())
                LoggedConsole.WriteLine("\tAudacity Path   :     " + this.AudacityExe.FullName);
            else
                LoggedConsole.WriteLine("\tAudacity Path   :     NOT FOUND!");

            if (WordPadExists())
                LoggedConsole.WriteLine("\tWordPad Path    :     " + this.WordPadExe.FullName);
            else
                LoggedConsole.WriteLine("\tWordPad Path    :     NOT FOUND!");
            LoggedConsole.WriteLine("\tDisplay:  Track Height={0}pixels. Tracks normalised={1}.", this.TrackHeight, this.TrackNormalisedDisplay);
            LoggedConsole.WriteLine("####################################################################################\n");
        } // WriteSettings2Console()


        public bool AudacityExists()
        {
            try // locate AUDACITY
            {
                this.AudacityExe = null;
                FileInfo audacity1 = AppConfigHelper.GetFile("AudacityExe1", false);
                if (audacity1.Exists)
                {
                    this.AudacityExe = audacity1;
                    return true;
                }
                FileInfo audacity2 = AppConfigHelper.GetFile("AudacityExe2", false);
                if (audacity2.Exists)
                {
                    this.AudacityExe = audacity2;
                    return true;
                }
                throw new FileNotFoundException();
            }
            catch (FileNotFoundException ex)
            {
                //MessageBox.Show(ex.ToString());
                return false;
            } //catch
        }

        public bool WordPadExists()
        {
            try // locate WordPad
            {
                FileInfo wordPad = AppConfigHelper.GetFile("WordPadExe", false);
                if (!wordPad.Exists)
                {
                    wordPad = null;
                    throw new FileNotFoundException();
                }
                this.WordPadExe = wordPad;
                return true;
            }
            catch (FileNotFoundException ex)
            {
                //MessageBox.Show(ex.ToString());
                return false;
            } //catch
        }

        public bool AnalysisProgramsExeExists()
        {
            try // locate Console exe
            {
                FileInfo exe = AppConfigHelper.GetFile("AnalysisProgramsExe", false);
                if (!exe.Exists)
                {
                    exe = null;
                    throw new FileNotFoundException();
                }
                this.AnalysisProgramsExe = exe;
                return true;
            }
            catch (FileNotFoundException ex)
            {
                //MessageBox.Show(ex.ToString());
                return false;
            } //catch
        }

        public bool ConsoleExists()
        {
            try // locate Console exe
            {
                FileInfo console = AppConfigHelper.GetFile("ConsoleExe", false);
                if (!console.Exists)
                {
                    console = null;
                    throw new FileNotFoundException();
                }
                this.ConsoleExe = console;
                return true;
            }
            catch (FileNotFoundException ex)
            {
                //MessageBox.Show(ex.ToString());
                return false;
            } //catch
        }
        
        public bool AudioAnalysisFilesExist()
        {
            //CHECK THESE FILES EXIST
            //<add key="AudioUtilityFfmpegExe" value="audio-utils\ffmpeg\ffmpeg.exe" />
            //    <add key="AudioUtilityFfprobeExe" value="audio-utils\ffmpeg\ffprobe.exe" />
            //    <add key="AudioUtilityWvunpackExe" value="audio-utils\wavpack\wvunpack.exe" />
            //    <add key="AudioUtilityMp3SpltExe" value="audio-utils\mp3splt\mp3splt.exe" />
            //    <add key="AudioUtilitySoxExe" value="audio-utils\sox\sox.exe" />
            //    <add key="AudioUtilityShntoolExe" value="audio-utils\shntool\shntool.exe" />
            try
            {
                var fiEXE = AppConfigHelper.GetFile("AudioUtilityFfmpegExe", true);
                fiEXE = AppConfigHelper.GetFile("AudioUtilityFfprobeExe", true);
                fiEXE = AppConfigHelper.GetFile("AudioUtilityWvunpackExe", true);
                fiEXE = AppConfigHelper.GetFile("AudioUtilityMp3SpltExe", true);
                fiEXE = AppConfigHelper.GetFile("AudioUtilitySoxExe", true);
                fiEXE = AppConfigHelper.GetFile("AudioUtilityShntoolExe", true);
            }
            catch (FileNotFoundException ex)
            {
                return false;
            } //catch
            return true;
        } // AudioAnalysisFilesExist()


        public FileInfo AudacityExe { get; private set; }
        public FileInfo WordPadExe { get; private set; }
        public FileInfo AnalysisProgramsExe { get; private set; }        
        public FileInfo ConsoleExe { get; private set; }       
        public int DefaultResampleRate { get; private set; }
        public double DefaultSegmentDuration { get; private set; }  //measured in minutes
        public double SonogramBackgroundThreshold { get; private set; }
        public int TrackHeight { get; private set; }
        public int TrackCount { get; private set; }
        public bool TrackNormalisedDisplay { get; private set; }
        public string SourceFileExt { get; private set; }
        public string AnalysisIdentifier { get; set; }
        public string[] AnalysisList { get; private set; }

        public DirectoryInfo DefaultSourceDir { get; private set; }
        public DirectoryInfo DefaultConfigDir { get; private set; }
        public DirectoryInfo DefaultOutputDir { get; private set; }
        public DirectoryInfo diSourceDir { get; set; }
        public DirectoryInfo diConfigDir { get; set; }
        public DirectoryInfo diOutputDir { get; set; }

        public DirectoryInfo DefaultTempFilesDir { get; set; }

        public FileInfo fiSourceRecording { get; set; }
        public FileInfo fiAnalysisConfig  { get; set; }
        public FileInfo fiCSVFile         { get; set; }
        public FileInfo fiSegmentRecording { get; set; }
    }
}
