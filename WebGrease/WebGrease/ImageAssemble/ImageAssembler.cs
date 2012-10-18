//--------------------------------------------------------------------- 
// <copyright file="ImageAssembler.cs" company="Microsoft"> 
// Copyright Microsoft Corporation, all rights reserved
// </copyright> 
// <summary> 
// Image Assemble Code ActivityMode takes care of assembling images referred in CSS files
// to the sprite images per image type.
// </summary> 
//---------------------------------------------------------------------

namespace WebGrease.ImageAssemble
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Image Assemble Task that invokes Image Assemble Tool 
    /// to assemble sprite images.
    /// </summary>
    internal sealed class ImageAssembler
    {
        #region Properties
        /// <summary>
        /// Gets or sets Input Directory (/inputdirectory or /i)
        /// </summary>
        /// <value>Input Directory path</value>
        public string InputDirectory { get; set; }

        /// <summary>
        /// Gets or sets Input Image File paths (/inputfilepaths or /f)
        /// </summary>
        /// <value>Semicolon separated Input Image file paths</value>
        public string InputFilePaths { get; set; }

        /// <summary>
        /// Gets or sets Sprite Image name (/spriteimage or /s)
        /// </summary>
        /// <value>Sprite Image Name</value>
        public string SpriteImage { get; set; }

        /// <summary>
        /// Gets or sets Log file name with path (/logfile or /l)
        /// </summary>
        /// <value>Log File name with path</value>
        public string LogFile { get; set; }

        /// <summary>
        /// Gets or sets Padding between images (/padding or /p)
        /// </summary>
        /// <value>Padding between images</value>
        public string Padding { get; set; }

        /// <summary>
        /// Gets or sets Output directory path (/outputdirectory)
        /// </summary>
        /// <value>Output directory path</value>
        public string OutputDirectory { get; set; }

        /// <summary>
        /// Gets or sets the command string to execute PNG optimizer Tool exe that
        /// will be used to optimize PNG sprite images.
        /// </summary>
        public string PngOptimizerToolCommand { get; set; }

        /// <summary>
        /// Gets or sets Packing type - Horizontal / Vertical (/packingscheme)
        /// </summary>
        /// <value>Packing scheme</value>
        public string PackingScheme { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to throw Exception from this task
        /// </summary>
        public bool ShouldThrowException { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to dedup images
        /// </summary>
        public bool Dedup { get; set; }

        /// <summary>
        /// Gets or sets List of InputImage objects for the Task.
        /// </summary>
        public IList<InputImage> InputImageList { get; set; }

        #endregion

        /// <summary>
        /// Executes the Image Assemble Code ActivityMode
        /// </summary>
        public void Execute()
        {
            try
            {
                var args = this.GenerateArgs();

                // Parse Arguments first
                ArgumentParser.ParseArguments(args, this.InputImageList == null ? null : this.InputImageList.ToList());

                var packingType = ArgumentParser.ParseSpritePackingType(ArgumentParser.ArgumentValueData[ArgumentParser.PackingScheme]);
                var assembledImageName = Path.Combine(ArgumentParser.ArgumentValueData[ArgumentParser.OutputDirectory], ArgumentParser.ArgumentValueData[ArgumentParser.SpriteName]);

                ImageAssembleGenerator.AssembleImages(ArgumentParser.InputImageList, packingType, assembledImageName, ArgumentParser.ArgumentValueData[ArgumentParser.XmlMapName], this.PngOptimizerToolCommand, bool.Parse(ArgumentParser.ArgumentValueData[ArgumentParser.Dedup]));
            }
            catch (Exception)
            {
                if (this.ShouldThrowException)
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Generates command line arguments.
        /// </summary>
        /// <returns>Command line arguments.</returns>
        private string[] GenerateArgs()
        {
            var args = new List<string>();
            if (!string.IsNullOrEmpty(this.InputDirectory))
            {
                args.Add(ArgumentParser.DirectoryName + this.InputDirectory);
            }
            else if (!string.IsNullOrEmpty(this.InputFilePaths))
            {
                args.Add(ArgumentParser.Paths + this.InputFilePaths);
            }

            if (!string.IsNullOrWhiteSpace(this.OutputDirectory))
            {
                args.Add(ArgumentParser.OutputDirectory + this.OutputDirectory);
            }

            if (!string.IsNullOrWhiteSpace(this.LogFile))
            {
                args.Add(ArgumentParser.XmlMapName + this.LogFile);
            }

            if (!string.IsNullOrWhiteSpace(this.Padding))
            {
                args.Add(ArgumentParser.Padding + this.Padding);
            }

            // Add Sprite Image Name if provided else It will be hashed by tool
            if (!string.IsNullOrEmpty(this.SpriteImage))
            {
                args.Add(ArgumentParser.SpriteName + this.SpriteImage);
            }
            
            // Add Packing scheme if provided else Vertical will be used by default
            if (!string.IsNullOrEmpty(this.PackingScheme))
            {
                args.Add(ArgumentParser.PackingScheme + this.PackingScheme);
            }

            args.Add(ArgumentParser.Dedup + this.Dedup);

            return args.ToArray();
        }
    }
}
