// <copyright file="TemplateCollection.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace AudioAnalysisTools.ContentDescriptionTools
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Acoustics.Shared;
    using Acoustics.Shared.ConfigFile;

    public class TemplateCollection : Dictionary<string, TemplateManifest>, IConfig
    {
        public static void CreateNewTemplatesManifest(FileInfo manifestFile)
        {
            // Read in all template manifests
            var templateCollection = ConfigFile.Deserialize<TemplateCollection>(manifestFile);
            var oldFile = new FileInfo(Path.Combine(manifestFile.DirectoryName ?? throw new InvalidOperationException(), "ContentDescriptionTemplates.Backup.yml"));
            Yaml.Serialize(oldFile, templateCollection);

            foreach (var manifest in templateCollection)
            {
                var template = manifest.Value;

                if (template.Status == TemplateStatus.Locked)
                {
                    continue;
                }

                template.MostRecentEdit = DateTime.Now;
            }

            Yaml.Serialize(manifestFile, templateCollection);
        }

        public event Action<IConfig> Loaded;

        public string ConfigPath { get; set; }

        void IConfig.InvokeLoaded()
        {
            this.Loaded?.Invoke(this);
        }

        //    public static Dictionary<string, double[,]> GetTemplateMatrices(TemplateCollection templates)
        //    {
        //        // init dictionary of matrices
        //        var opTemplate = new Dictionary<string, double[,]>();

        //        foreach (var template in templates)
        //        {
        //            var name = template.Key;
        //            var templateData = template.Value;
        //            var dataDict = templateData.Template;

        //            // init a matrix to contain template values
        //            var matrix = new double[,];
        //            foreach (var kvp in dataDict)
        //            {
        //                var array = kvp.Value;
        //                matrix.AddRow();
        //            }

        //            opTemplate.Add(name, matrix);
        //        }

        //        return opTemplate;
        //    }
    }
}
