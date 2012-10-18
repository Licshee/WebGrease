// ----------------------------------------------------------------------------------------------------
// <copyright file="TempFile.cs" company="">
//   
// </copyright>
// ----------------------------------------------------------------------------------------------------

namespace WebGrease.Tests
{
    using System;
    using System.IO;

    /// <summary>
    /// The tempfile class is only used in unit tests.
    /// It creates a temprorary file in the current working folder and has a property that points to the new filename.
    /// It will remove the file once the processing is done.
    /// Use it like:
    /// using (var tempFile = new TempFile("somefileName"))
    /// {
    ///     // Run tests on the temp file
    /// }
    /// </summary>
    public class TempFile : IDisposable
    {
        #region Constructors and Destructors
        ///
        public TempFile(string content, string filename)
        {
            this.Filename = filename;
            using (var file = File.CreateText(filename))
            {
                file.Write(content);
            }
        }

        #endregion

        #region Public Properties

        public string Filename { get; private set; }

        #endregion

        #region Public Methods and Operators

        public void Dispose()
        {
            try
            {
                File.Delete(this.Filename);
            }
            catch (Exception)
            {
            }
        }

        #endregion
    }
}