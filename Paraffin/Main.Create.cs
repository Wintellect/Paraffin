//------------------------------------------------------------------------------
// <copyright file= "Main.Create.cs" company="Wintellect">
//    Copyright (c) 2002-2017 John Robbins/Wintellect -- All rights reserved.
// </copyright>
// <Project>
//    Wintellect Debugging .NET Code
// </Project>
//------------------------------------------------------------------------------

namespace Wintellect.Paraffin
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// All the methods related to new file creation.
    /// </summary>
    internal static partial class Program
    {
        /// <summary>
        /// Creates a brand new .WXS file for the specified directory and 
        /// options. Any previous file of this name is overwritten.
        /// </summary>
        /// <returns>
        /// Zero if the file was all properly written.
        /// </returns>
        private static Int32 CreateNewFile()
        {
            verboseOut.TraceInformation(Constants.VerboseCreateFile,
                                        argValues.FileName);

            // Create the XML document.
            XDocument doc = new XDocument();

            // Add the WiX and Fragment nodes.
            XElement root = new XElement(argValues.WixNamespace + "Wix");
            doc.Add(root);
            XElement fragment = new XElement(argValues.WixNamespace + "Fragment");
            root.Add(fragment);

            // Add the DirectoryRef node.
            XElement directoryRef = new XElement(argValues.WixNamespace + "DirectoryRef",
                                 new XAttribute("Id", argValues.DirectoryRef));
            fragment.Add(directoryRef);

            // Get the starting directories initialized.
            InitializeDirectoryValues();

            // Now start the grind.
            RecurseDirectoriesForNewFile(directoryRef, fullStartDirectory);

            // Add the Component group node.
            AddComponentGroup(fragment);

            // Add any include elements.
            AddIncludeFiles(root);

            // Add the comment with all the command line options.
            AddCommandLineOptionsComment(root);

            // We're done, save it!
            doc.Save(argValues.FileName);
            return 0;
        }

        /// <summary>
        /// Called when processing a new file or a directory that's wasn't seen
        /// in the existing .WXS when updating.
        /// </summary>
        /// <param name="currElement">
        /// The current element in the output XML document.
        /// </param>
        /// <param name="directory">
        /// The disk directory to recurse.
        /// </param>
        private static void RecurseDirectoriesForNewFile(XElement currElement,
                                                         String directory)
        {
            verboseOut.TraceInformation(Constants.VerboseProcessNewDirectory,
                                        directory);

            // If the -norootdirectory switch is set, the user does not want to
            // add the <Directory> element to the fragment I'm creating.
            // That means I'll be adding files to the <DirectoryRef> node, which
            // has been passed in the currElement. 
            XElement addToNode = currElement;
            if (!argValues.NoRootDirectoryState)
            {
                // It's new so create a Directory element.
                XElement directoryNode = CreateDirectoryElement(directory);

                // Add the current directory to the passed in element
                addToNode.Add(directoryNode);

                // Now I'm adding to this directory node.
                addToNode = directoryNode;
            }
            else
            {
                // This is the first time through with the -norootdirectory 
                // switch so set it to false so any directories recursed will 
                // show up.
                argValues.NoRootDirectoryState = false;
            }

            // Add the files to this directory node.
            AddNewFilesToDirectoryNode(directory, addToNode);

            // Recurse the directories if I'm supposed to do so.
            if (!argValues.NoRecursion)
            {
                String[] dirs = Directory.GetDirectories(directory);
                foreach (String item in dirs)
                {
                    Boolean skipDirectory = IsDirectoryExcluded(item);
                    if (false == skipDirectory)
                    {
                        RecurseDirectoriesForNewFile(addToNode, item);
                    }
                }
            }
        }

        /// <summary>
        /// For new directories when creating new files or when adding new
        /// directories when processing an existing .WXS, adds the files
        /// to the <see cref="directoryElem"/> element.
        /// </summary>
        /// <param name="directory">
        /// The directory to get the files from.
        /// </param>
        /// <param name="directoryElem">
        /// The Director element to add the new Component/File elements to.
        /// </param>
        private static void AddNewFilesToDirectoryNode(String directory,
                                                       XElement directoryElem)
        {
            IEnumerable<String> filesQuery = ProcessedDirectoryFiles(directory);
            String[] files = filesQuery as String[] ?? filesQuery.ToArray();
            if (files.Length == 0)
            {
                return;
            }

            // Create the first Component element. 
            XElement currentComponent = CreateComponentElement();

            // For each file on disk.
            foreach (String file in files)
            {
                // Create the File element and add it to the current
                // Component element.
                XElement fileElement = CreateFileElement(file);
                currentComponent.Add(fileElement);

                if (argValues.PerUser)
                {
                    XElement registryValueElement = CreateRegistryValueElement(file);
                    currentComponent.Add(registryValueElement);
                }

                directoryElem.Add(currentComponent);
                currentComponent = CreateComponentElement();
            }

            // Look for any files the user wants to inject contents from 
            // this directory.
            XElement addToNode = directoryElem;

            AddMoldFilesContentToNode(directory, addToNode);

            // I'm done with this directory so bump up the component and 
            // directory count if the user asked for that to happen. This 
            // is for compatibility with version 1 files.
            componentNumber += argValues.IncrementValue - 2;
        }
    }
}
