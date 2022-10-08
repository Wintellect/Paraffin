//------------------------------------------------------------------------------
// <copyright file= "Main.Update.cs" company="Wintellect">
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
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    /// <summary>
    /// All the methods related to updating files.
    /// </summary>
    internal static partial class Program
    {
        /// <summary>
        /// Takes an existing .WXS file and generates an updated version, which
        /// is saved to a .PARAFFIN extension.
        /// </summary>
        /// <returns>
        /// 0 - The .PARAFFIN file was created.
        /// 2 - The input file does not have the special comment in the 
        /// appropriate location.
        /// </returns>
        private static Int32 UpdateExistingFile()
        {
            verboseOut.TraceInformation(Constants.VerboseUpdateFile,
                                        argValues.FileName);
            Int32 returnValue = 0;

            // Load the XML document. Any loading problems go right
            // to an exception for the user.
            XDocument inputDoc = XDocument.Load(argValues.FileName);

            // The output filename.
            String outputFile = Path.ChangeExtension(argValues.FileName, ".PARAFFIN");

            // The first node has to be comment I put there when 
            // the file was created.
            if (inputDoc.Root.FirstNode is XComment options)
            {
                // It's a comment node, so set all the arguments from that 
                // section.
                Boolean ret = InitializeArgumentsFromFile(options.Value);

                if (ret)
                {
                    // Create the new output file.
                    XDocument outputDoc = new XDocument();

                    // Add the top level WiX node, but before we do, make sure
                    // to copy over any additional namespaces the use may have
                    // added manually to the new file.
                    List<XAttribute> attrs = new List<XAttribute>();
                    foreach (var item in inputDoc.Root.Attributes())
                    {
                        // Only add the custom namespaces, not the regular WiX
                        // namespace.
                        if (String.Compare(argValues.WixNamespace.NamespaceName,
                                           item.Value,
                                           StringComparison.CurrentCultureIgnoreCase) != 0)
                        {
                            verboseOut.TraceInformation(Constants.VerboseAddingNamespace,
                                                        item.Name.LocalName,
                                                        item.Value);
                            attrs.Add(
                                new XAttribute(XNamespace.Xmlns + item.Name.LocalName,
                                               item.Value));
                        }
                    }

                    // The WixNamespace+"Wix" will get us the default namespace.
                    XElement outputRoot = new XElement(argValues.WixNamespace + "Wix", attrs);
                    outputDoc.Add(outputRoot);

                    // Add the Fragment node.
                    XElement outputFragment = new XElement(argValues.WixNamespace + "Fragment");
                    outputRoot.Add(outputFragment);

                    // Find the directory ref of the input file.
                    XElement inputDirRef = inputDoc.Descendants(argValues.WixNamespace + "DirectoryRef").First();
                    String idValue = inputDirRef.Attributes("Id").First().Value;

                    // Build a DirectoryRef for the output file.
                    XElement outputDirRef = new XElement(argValues.WixNamespace + "DirectoryRef",
                                                         new XAttribute("Id", idValue));

                    // Add the directory ref to the output file.
                    outputFragment.Add(outputDirRef);

                    // Get the starting directory values ready to go.
                    InitializeDirectoryValues();

                    // Recurse through the input file and the directories 
                    // themselves.
                    RecurseDirectoriesForExistingFile(inputDirRef,
                                                      outputDirRef,
                                                      fullStartDirectory);

                    // Add the Component group node.
                    AddComponentGroup(outputFragment);

                    // Add any include elements.
                    AddIncludeFiles(outputRoot);

                    // Add the comment with all the command line options.
                    AddCommandLineOptionsComment(outputRoot);

                    // All OK, Jumpmaster!
                    outputDoc.Save(outputFile);

                    // Does the user want to check for file differences?
                    if (argValues.ReportIfDifferent)
                    {
                        Boolean retCheck = CheckIfDifferent(argValues.FileName,
                                                            outputFile);
                        if (retCheck)
                        {
                            returnValue = 4;
                        }
                    }
                }
                else
                {
                    errorMessage = Constants.ErrorMultipleFilesPerComponent;
                    returnValue = 3;
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

        /// <summary>
        /// Does the work of recursing both the file system directories and the 
        /// original .WXS file to produce an updated XML document.
        /// </summary>
        /// <param name="currInputElement">
        /// The current element in the input .WXS file.
        /// </param>
        /// <param name="currOutputElement">
        /// The current element in the output .PARAFFIN file.
        /// </param>
        /// <param name="directory">
        /// The directory to process. This has to be the full directory value.
        /// </param>
        /// <remarks>
        /// As you can guess, this is called recursively.
        /// </remarks>
        private static void RecurseDirectoriesForExistingFile(XElement currInputElement,
                                                              XElement currOutputElement,
                                                              String directory)
        {
            // If the currInputElement is null, I'm processing a brand new 
            // directory that isn't in the original file. Thus, I can treat 
            // adding this directory just like it's a new file and add this 
            // directory, plus all under it.
            if (null == currInputElement)
            {
                RecurseDirectoriesForNewFile(currOutputElement, directory);
            }
            else
            {
                verboseOut.TraceInformation(Constants.VerboseProcessUpdateDirectory,
                                            directory);

                // The directory element I'm going to be building up.
                XElement outputDirElement;

                // Get the directory info in order to get just the name.
                DirectoryInfo info = new DirectoryInfo(directory);
                String name = info.Name;

                // Does this directory already exist in the input file? 
                var findDirectory = from elem in currInputElement.Elements()
                                    where
                          String.Compare(name,
                                          (String)elem.Attribute("Name"),
                                          true,
                                          CultureInfo.CurrentCulture) == 0
                                    select elem;

                XElement inputDirElement = null;
                Int32 fileCount = findDirectory.Count();
                Debug.Assert(fileCount <= 1, "fileCount <= 1");
                if (fileCount > 1)
                {
                    // We've got a serious problem. :( You can't have multiple
                    // directories with the same name.
                    String err = String.Format(CultureInfo.CurrentCulture,
                                               Constants.InvalidFileNameCountFmt,
                                               name);
                    throw new InvalidOperationException(err);
                }

                if (0 == fileCount)
                {
                    if (argValues.NoRootDirectoryState == false)
                    {
                        // This is a new directory.
                        outputDirElement = CreateDirectoryElement(directory);

                        // Add this element to the output element.
                        currOutputElement.Add(outputDirElement);
                    }
                    else
                    {
                        // If the -norootdirectory option is true, there is no 
                        // <Directory> element in the input file. I'm going to 
                        // put the files directly into the <DirectoryRef> 
                        // element, which is the currOutputElement so there's 
                        // nothing else to do here.
                        outputDirElement = currOutputElement;

                        // Have the inputDirElement point to the input file
                        // <DirectoryRef> element.
                        inputDirElement = currInputElement;

                        // As this is the first time through, set the flag to 
                        // false so recursed directories will have their 
                        // <Directory> nodes included.
                        argValues.NoRootDirectoryState = false;
                    }
                }
                else
                {
                    // We've got one element so grab it.
                    inputDirElement = findDirectory.First();

                    // This directory was in the previous file so copy it's 
                    // attributes over to the new file.
                    outputDirElement = new XElement(argValues.WixNamespace + "Directory");
                    foreach (var attrib in inputDirElement.Attributes())
                    {
                        outputDirElement.SetAttributeValue(attrib.Name,
                                                           attrib.Value);
                    }

                    // Add this element to the output element.
                    currOutputElement.Add(outputDirElement);
                }

                // Process all the files in this directory as compared to the 
                // input file.
                UpdateFilesInDirectoryNode(directory,
                                           inputDirElement,
                                           outputDirElement);

                // Recurse directories if the original file had that set.
                if (false == argValues.NoRecursion)
                {
                    String[] dirs = Directory.GetDirectories(directory);
                    foreach (var item in dirs)
                    {
                        // Is this a directory the user wanted to skip?
                        Boolean skipDirectory = IsDirectoryExcluded(item);
                        if (false == skipDirectory)
                        {
                            RecurseDirectoriesForExistingFile(
                                                            inputDirElement,
                                                            outputDirElement,
                                                            item);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Looks at the input .WXS for the files in this directory and compare
        /// it to the files on disk. If the file is the same or is a new file,
        /// add the files to the output. If it's no longer present on disk, but
        /// is in the input .WXS, skip adding the file to the output.
        /// </summary>
        /// <param name="directory">The disk directory to scan.</param>
        /// <param name="inputDir">The Directory element from the .WXS file that
        /// maps to <see cref="directory"/>.</param>
        /// <param name="outputDir">The Directory element for the output 
        /// .PARAFFIN file.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if there's multiple files with the same name in the 
        /// <paramref name="outputDir"/> child elements.
        /// </exception>
        private static void UpdateFilesInDirectoryNode(String directory,
                                                       XElement inputDir,
                                                       XElement outputDir)
        {
            // If the inputDir element is null, just treat this as if I was 
            // creating a new file.
            if (null == inputDir)
            {
                AddNewFilesToDirectoryNode(directory, outputDir);
            }
            else
            {
                // The .WXS file had files for this directory so I need to 
                // match them all up with the existing files.

                // Start by getting the files in this directory.
                var filesQuery = ProcessedDirectoryFiles(directory);

                // I'll add to the output .PARAFFIN file current Directory node 
                // by default.
                XElement addToElement = outputDir;

                // If there's no files in this directory, there's nothing else 
                // to do.
                if (0 != filesQuery.Count())
                {
                    // In case the user wants the .PARAFFIN file to contain
                    // the transitive patch information.
                    List<XElement> processedFiles = new List<XElement>();

                    // First get the child component(s) from this Directory.
                    var comps = inputDir.Elements(argValues.WixNamespace + "Component");

                    // Now get all the files from just these Component 
                    // elements.
                    var inputFiles = comps.Descendants(argValues.WixNamespace + "File");

                    // Loop through all the files on disk.
                    foreach (var file in filesQuery)
                    {
                        // Get the aliased value for this file.
                        String aliasedName = AliasedFilename(file);

                        // See if I can find that file in the input .WXS file 
                        // by checking the aliased Source names.
                        var inputFileQuery = from fileNode in inputFiles
                                             where
                               String.Compare(aliasedName,
                                       (String)fileNode.Attribute("Source"),
                                               true,
                                               CultureInfo.CurrentCulture) == 0
                                             select fileNode;
                        Int32 fileCount = inputFileQuery.Count();
                        Debug.Assert(fileCount <= 1, "fileCount <= 1");
                        if (0 == fileCount)
                        {
                            // Put this file element in it's own Component.
                            // The component always has to be created first.
                            // so that the component and file are using the
                            // same unique number.
                            XElement compElement = CreateComponentElement();

                            // This is a new file that wasn't in the input .WXS
                            // file so just add it. First create a new File 
                            // element.
                            XElement fileElement = CreateFileElement(file);
                            compElement.Add(fileElement);

                            if (argValues.PerUser)
                            {
                                XElement registryValueElement = CreateRegistryValueElement(file);
                                compElement.Add(registryValueElement);
                            }

                            // Add this element to the directory.
                            addToElement.Add(compElement);
                        }
                        else if (1 == fileCount)
                        {
                            // This gets me the file element.
                            var fileElement = inputFileQuery.First();

                            // Now I've got the component as well.
                            var compElement = fileElement.Parent;

                            // With the transitive patching, I want to be 
                            // careful about adding the node. If the file does
                            // not exist on the update, the component node is 
                            // added to the end of the components. On the 
                            // next update run, if the zero byte file exists, 
                            // I don't want to add it here because it'd now be 
                            // in a different location in the file. That would 
                            // break difference comparisons (like the 
                            // -ReportIfDifferent switch) as the file contents, 
                            // which identical from a WiX perspective, are not 
                            // identical from a logical perspective.

                            // Go through the logic to see if I need to skip 
                            // the file.
                            Boolean skip = SkipFile(compElement, file);
                            if (false == skip)
                            {
                                // If the user wants transitive patches, add 
                                // this file to the list of files that I've 
                                // processed.
                                if (argValues.PatchUpdate)
                                {
                                    processedFiles.Add(fileElement);
                                }

                                // Prior to version 3.5, I put the KeyPath 
                                // attribute on the Component, but it's best on 
                                // the File. If the File doesn't have the 
                                // KeyPath, I'll add it here.
                                FixKeyPathAttribute(file,
                                                    fileElement,
                                                    compElement);

                                // Add the file element to the parent node.
                                addToElement.Add(compElement);
                            }
                        }
                        else
                        {
                            // There's multiple files with the same name in this
                            // particular node. That's bad. :(
                            String err = String.Format(
                                        CultureInfo.CurrentCulture,
                                        Constants.InvalidFileNameCountFmt,
                                        file);
                            throw new InvalidOperationException(err);
                        }
                    }

                    // Does the user want to apply transitive patches to this 
                    // update?
                    if (argValues.PatchUpdate)
                    {
                        PatchUpdates(inputDir, processedFiles, addToElement);
                    }
                }

                // Look for any files the user wants to inject contents from 
                // this directory.
                AddMoldFilesContentToNode(directory, addToElement);
            }
        }

        /// <summary>
        /// Processes any possible transitive patch nodes for files removed 
        /// from the installer.
        /// </summary>
        /// <param name="inputDir">
        /// The Directory element in the .WXS file being processed.
        /// </param>
        /// <param name="processedFiles">
        /// The list of File elements that have already been added to the output
        /// .PARAFFIN file.
        /// </param>
        /// <param name="outDir">
        /// The Directory element in the output .PARAFFIN file being created.
        /// </param>
        private static void PatchUpdates(XElement inputDir,
                                         List<XElement> processedFiles,
                                         XElement outDir)
        {
            // First get the child component(s) from this Directory.
            var comps = inputDir.Elements(argValues.WixNamespace + "Component");

            // Now get all the files from just these Component 
            // elements.
            var inputFiles = comps.Descendants(argValues.WixNamespace + "File");

            // Get the difference between the input files and those that were 
            // just processed. That gives me the deleted files.
            var deletedFiles = inputFiles.Except(processedFiles);

            // If there's any deleted files, copy them over, set the transitive
            // attribute on the Component, and make the Condition element a 
            // child.
            foreach (var file in deletedFiles)
            {
                var parentComponent = file.Parent;

                // Is there already a Transitive attribute on this Component?
                // If not, put one there and add the condition.
                var attrib = parentComponent.Attribute("Transitive");
                if (null == attrib)
                {
                    parentComponent.Add(new XAttribute("Transitive", "yes"));
                    parentComponent.Add(new XElement(argValues.WixNamespace + "Condition", "1 = 0"));
                }

                outDir.Add(parentComponent);

                String fileName = UnAliasedFilename(file.Attribute("Source").Value);

                verboseOut.TraceInformation(Constants.VerboseFileRemoved,
                                            fileName);

                // Does the user want the files created, too?
                if (argValues.PatchCreateFiles)
                {
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

        /// <summary>
        /// Does the logic to determine if I'm supposed to skip this node so 
        /// any Component elements with the Transitive attribute always appear 
        /// in the same place in the file.
        /// </summary>
        /// <param name="compElement">
        /// The Component element for the <paramref name="file"/>.
        /// </param>
        /// <param name="file">
        /// The file to look at.
        /// </param>
        /// <returns>
        /// True to skip the file.
        /// </returns>
        private static Boolean SkipFile(XElement compElement, String file)
        {
            // If we're not doing transitive patch upgrades, don't skip this 
            // file.
            if (false == argValues.PatchUpdate)
            {
                return false;
            }

            // We are doing transitive patches so does the component already 
            // have a Transitive attribute on it?
            XAttribute transAttr = compElement.Attribute("Transitive");
            if (null == transAttr)
            {
                return false;
            }

            // Finally, check the length of the file. If it's zero, I don't want
            // to do this node as I want it added at the end of the Directory
            // element to keep logical consistency.
            FileInfo info = new FileInfo(file);
            if (0 == info.Length)
            {
                return true;
            }

            // I want to do one last check here. If the Transitive attribute
            // is set AND the file is larger than zero bytes, there's trouble.
            // This means the user is adding back a file that had been deleted.
            // As that will probably completely screw up component rules, I'm
            // going to abort here.
            if (info.Length > 0)
            {
                String msg = String.Format(CultureInfo.CurrentCulture,
                                 Constants.AttemptingToAddPreviouslyDeletedFile,
                                 file);
                throw new InvalidOperationException(msg);
            }

            return false;
        }

        /// <summary>
        /// Prior to Paraffin 3.5, I put the KeyPath attribute on the Component
        /// instead of the File. This switches them around to make updating 
        /// files created with a previous version easier.
        /// </summary>
        /// <param name="file">
        /// The filename currently being processed. Used to report the update to
        /// the user.
        /// </param>
        /// <param name="fileElement">
        /// The File element to fix.
        /// </param>
        /// <param name="compElement">
        /// The Component element to fix
        /// </param>
        private static void FixKeyPathAttribute(String file,
                                                XElement fileElement,
                                                XElement compElement)
        {
            var attrib = fileElement.Attribute("KeyPath");
            if (attrib == null && !argValues.PerUser) // When PerUser, KeyPath is stored on a RegistryValue
            {
                fileElement.Add(
                          new XAttribute("KeyPath", "yes"));
                WriteWarning(Constants.AddingKeyPathToFile,
                             file);
            }

            // Check to see if the Component has a KeyPath
            // attribute and if so, remove it.
            attrib = compElement.Attribute("KeyPath");
            if (attrib != null)
            {
                compElement.SetAttributeValue("KeyPath",
                                              null);
                WriteWarning(
                      Constants.RemovedKeyPathFromComponent,
                      compElement.Attribute("Id").Value);
            }
        }

        /// <summary>
        /// Initializes the <see cref="ParaffinArgParser"/> with all the
        /// settings from the first comment block. Used when reading in a .WXS
        /// to compare to the files on the disk.
        /// </summary>
        /// <param name="inputXml">
        /// The XML string to process.
        /// </param>
        /// <returns>
        /// True if everything is cool, false if this is a multiple files
        /// per component file.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", 
                                                         "CA1502:AvoidExcessiveComplexity", 
                                                         Justification ="Want to keep this processing together for easy readability")]
        private static Boolean InitializeArgumentsFromFile(String inputXml)
        {
            XElement options = XElement.Parse(inputXml);

            // Save off the settings from the command line.
            ParaffinArgParser originalArgs = argValues;

            // Start the arguments from the comment section.
            argValues = new ParaffinArgParser
            {
                ReportIfDifferent = originalArgs.ReportIfDifferent,
                PatchUpdate = originalArgs.PatchUpdate,
                FileName = originalArgs.FileName,
                PatchCreateFiles = originalArgs.PatchCreateFiles
            };

            // Look for the version element. If it's missing or 1, it's an old
            // file.
            var verElem = options.Descendants(VERSIONELEM);
            if (verElem.Count() == 1)
            {
                Int32 ver = Convert.ToInt32(verElem.First().Value,
                                              CultureInfo.InvariantCulture);
                if (ver > argValues.Version)
                {
                    throw new InvalidOperationException(
                                                 Constants.MadeWithNewParaffin);
                }

                // In file version 2, the <GroupName> is what I need to read.
                // For previous versions it was <Custom>
                argValues.GroupName =
                               options.Descendants(GROUPNAMEELEM).First().Value;
            }
            else
            {
                // This is a file from 3.0 or prior.
                argValues.Version = Version1File;

                // These options are deprecated in newer versions, but I don't
                // want to break old files that are being updated.
                argValues.IncrementValue = Convert.ToInt32(options.Descendants(INCREMENTELEM).First().Value,
                                                           CultureInfo.InvariantCulture);

                directoryNumber = Convert.ToInt32(options.Descendants(NEXTDIRECTORYNUMELEM).First().Value,
                                                  CultureInfo.InvariantCulture);

                componentNumber = Convert.ToInt32(options.Descendants(NEXTCOMPONENTNUMBER).First().Value,
                                                  CultureInfo.InvariantCulture);

                // The <Custom> value was used in older versions.
                argValues.GroupName = options.Descendants(CUSTOMELEM).First().Value;
            }

            // Get all the easy values out.
            argValues.Alias = options.Descendants(ALIASELEM).First().Value;

            // The old multiple files per component option is no longer 
            // supported.
            var mul = options.Descendants(MULTIPLEELEM);
            if (mul.Count() != 0)
            {
                Boolean value = Convert.ToBoolean(mul.First().Value,
                                                  CultureInfo.InvariantCulture);
                if (value)
                {
                    return false;
                }
            }

            argValues.StartDirectory = options.Descendants(DIRECTORYELEM).First().Value;

            argValues.NoRecursion = Convert.ToBoolean(options.Descendants(NORECURSELEM).First().Value,
                                                      CultureInfo.InvariantCulture);

            var extNode = options.Descendants(EXTEXCLUDEELEM);
            foreach (var item in extNode.Descendants())
            {
                argValues.ExtensionList.Add(item.Value, true);
            }

            var dirEx = options.Descendants(DIREEXCLUDEELEM);
            foreach (var item in dirEx.Descendants())
            {
                argValues.DirectoryExcludeList.Add(item.Value);
            }

            // After releasing 1.0, I've added a few command line options. 
            // Since I don't want to break existing PARAFFIN generated files,
            // I'll not require the following options to be in existing files.
            // If they are cool, but no sense crashing out if they aren't.
            var win64Elems = options.Descendants(WIN64ELEM);
            if (win64Elems.Count() == 1)
            {
                // Check to see if this is an older file that uses "false" for
                // the value.
                String rawValue = win64Elems.First().Value;
                if (0 == String.Compare(rawValue,
                                        "false",
                                        StringComparison.OrdinalIgnoreCase))
                {
                    argValues.Win64 = String.Empty;
                }
                else
                {
                    // Grab the value.
                    argValues.Win64 = win64Elems.First().Value;
                }
            }
            else
            {
                // Assume false.
                argValues.Win64 = null;
            }

            var noDirElems = options.Descendants(NODIRECTORYELEM);
            if (noDirElems.Count() == 1)
            {
                argValues.NoRootDirectory = Convert.ToBoolean(noDirElems.First().Value,
                                                              CultureInfo.InvariantCulture);
                argValues.NoRootDirectoryState = argValues.NoRootDirectory;
            }

            var diskIdValue = options.Descendants(DISKIDELEM);
            if (diskIdValue.Count() == 1)
            {
                argValues.DiskId = Convert.ToInt32(diskIdValue.First().Value,
                                                  CultureInfo.InvariantCulture);
            }

            var permanentValue = options.Descendants(PERMANENT);
            if (permanentValue.Count() == 1)
            {

                argValues.Permanent = Convert.ToBoolean(options.Descendants(PERMANENT).First().Value, 
                                                        CultureInfo.InvariantCulture);
            }

            var wix4Usage = options.Descendants(WIX4);
            if (wix4Usage.Count() == 1)
            {
                String rawValue = wix4Usage.First().Value;
                if (0 == String.Compare(rawValue,
                                        "true",
                                        StringComparison.OrdinalIgnoreCase))
                {
                    argValues.WiX4 = true;
                }
            }

            var includeFileNode = options.Descendants(INCLUDEFILESELEM);
            foreach (var item in includeFileNode.Descendants())
            {
                argValues.IncludeFiles.Add(item.Value);
            }

            var regExFileNode = options.Descendants(REGEXEXELEMENT);
            foreach (var item in regExFileNode.Descendants())
            {
                argValues.RegExExcludes.Add(new Regex(item.Value,
                                                      RegexOptions.IgnoreCase));
            }

            var perUserUsage = options.Descendants(PERUSER);
            if (perUserUsage.Count() == 1)
            {
                String rawValue = perUserUsage.First().Value;
                if (0 == String.Compare(rawValue,
                                        "true",
                                        StringComparison.OrdinalIgnoreCase))
                {
                    argValues.PerUser = true;
                }
            }


            // Now that everything is read out of the original options block,
            // add in any additional -ext, -dirExclude, and -regExExclude 
            // options specified on the command line.
            foreach (var cmdLineExt in originalArgs.ExtensionList.Keys)
            {
                if (false == argValues.ExtensionList.ContainsKey(cmdLineExt))
                {
                    argValues.ExtensionList.Add(cmdLineExt, true);
                }
            }

            foreach (var dirExclude in originalArgs.DirectoryExcludeList)
            {
                if (false == argValues.DirectoryExcludeList.Contains(dirExclude))
                {
                    argValues.DirectoryExcludeList.Add(dirExclude);
                }
            }

            foreach (var regExExclude in originalArgs.RegExExcludes)
            {
                if (false == argValues.RegExExcludes.Any(rx => rx.ToString() == regExExclude.ToString()))
                {
                    argValues.RegExExcludes.Add(regExExclude);
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the input .WXS and output .PARAFFIN file are different.
        /// </summary>
        /// <param name="inputFile">
        /// The input .WXS file.
        /// </param>
        /// <param name="outputFile">
        /// The output .PARAFFIN file.
        /// </param>
        /// <returns>
        /// Returns true if the files are different.
        /// </returns>
        private static Boolean CheckIfDifferent(String inputFile,
String outputFile)
        {
            // Go ahead and laugh. :) When I looked at all the work it would
            // take to keep track of differences while processing the XML, I 
            // realized it was far too hard to deal with. Sometimes the simplest
            // thing to do is just be simple.
            String inputText = File.ReadAllText(inputFile);
            String outputText = File.ReadAllText(outputFile);
            Int32 val = String.Compare(inputText,
                                       outputText,
                                       StringComparison.CurrentCulture);
            return val != 0;
        }
    }
}
