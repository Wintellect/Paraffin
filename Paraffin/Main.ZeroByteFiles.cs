//------------------------------------------------------------------------------
// <copyright file= "Main.ZeroByteFiles.cs" company="Wintellect">
//    Copyright (c) 2002-2017 John Robbins/Wintellect -- All rights reserved.
// </copyright>
// <Project>
//    Wintellect Debugging .NET Code
// </Project>
//------------------------------------------------------------------------------

namespace Wintellect.Paraffin
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// All the methods related to updating files.
    /// </summary>
    internal partial class Program
    {
        private static Int32 CreateZeroByteFiles()
        {
            verboseOut.TraceInformation(Constants.VerboseCreateZeroFiles,
                                        argValues.FileName);
            Int32 returnValue = 0;

            // Load the XML document. Any loading problems go right
            // to an exception for the user.
            XDocument inputDoc = XDocument.Load(argValues.FileName);

            // The first node has to be comment I put there when 
            // the file was created.
            if (inputDoc.Root.FirstNode is XComment options)
            {
                // It's a comment node, so set all the arguments from that 
                // section.
                Boolean ret = InitializeArgumentsFromFile(options.Value);

                if (ret)
                {
                    // Get the starting directory values ready to go.
                    InitializeDirectoryValues();

                    // Grab all the File elements.
                    var files = inputDoc.Descendants(argValues.WixNamespace + "File");

                    // Get just the files with Component parents that have the 
                    // Transitive value set.
                    var removedFiles = from fileNode in files
                                       where
                                         fileNode.Parent.Attribute("Transitive") != null
                                       select fileNode;

                    foreach (var file in removedFiles)
                    {
                        String fileName = UnAliasedFilename(
                                                        file.Attribute("Source").Value);
                        if (false == File.Exists(fileName))
                        {
                            using (File.Create(fileName))
                            {
                                verboseOut.TraceInformation(
                                                      Constants.VerboseZeroByteFile,
                                                      fileName);
                            }
                        }
                    }
                }
            }
            else
            {
                // This does not look like a file this tool previously 
                // generated.
                errorMessage = Constants.UnknownFileType;
                returnValue = 2;
            }

            return returnValue;
        }
    }
}
