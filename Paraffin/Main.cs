//------------------------------------------------------------------------------
// <copyright file= "Main.cs" company="Wintellect">
//    Copyright (c) 2002-2017 John Robbins/Wintellect -- All rights reserved.
// </copyright>
// <Project>
//    Wintellect Debugging .NET Code
// </Project>
//------------------------------------------------------------------------------

/*------------------------------------------------------------------------------
 * See the following blog entries for more information about PARAFFIN:
 * 
 * http://www.wintellect.com/CS/blogs/jrobbins/archive/tags/Paraffin/default.aspx
 * 
 * 1.00 - Initial release
 * 1.01 - Fixed a bug where directory and component names could have a dash in 
 *        them, which is not supported by WiX.
 * 1.02 - Special thanks to Darren Stone for all his input about PARAFFIN.
 *      - Added -Win64 switch, which adds Win64="yes" to all components.
 *      - Updated the Id naming to keep all values in the range [0-9a-zA-Z_] to
 *        avoid any naming problems. WiX is not consistent on exactly what can
 *        characters can be in the Id attribute.
 *      - When updating, I was previously only relying on the Directory and 
 *        File elements Name attribute to find those elements. I mistakenly 
 *        thought the short file/directory name was guaranteed to be unique.
 *        I fixed this bug by updating the Directory element searching to look 
 *        for either the matching Name attribute or LongName attribute depending
 *        if the long name is different than the short name. For File elements, 
 *        I look at both the Name and the Source attributes for the exact match.
 *      - Fixed a bug where I was not properly matching directory names when
 *        generating the Id attribute.
 *      - Fixed the innocuous bug where I was appending a double slash on an
 *        alias if the input directory did not end in a trailing slash.
 * 1.03 - Fixed a bug where I was assuming that the short name for a file was
 *        constant. It's really a random value. Now I only look at the Source
 *        attribute when updating a File node as there's no other way to ensure
 *        that a file is the same. This means I might have a rare conflict with 
 *        the short name for a file. The big reason for upgrading to WiX 3.0 is
 *        that you no longer need to mess with these darn short names!
 * 1.04 - Thanks to Matthew Goos, added the -dirref option to allow a custom 
 *        name for the DirectoryRef node when creating a file.
 *      - Now the -ext and direXclude command line options can also be specified 
 *        for updates in order to add additional extensions or directories to 
 *        ignore when updating a file.
 * 3.00 - Sorry for the big version jump but Paraffin now targets WiX 3.0 so I
 *        thought I'd make them the same. Now that WiX 3.0 has hit beta, it's 
 *        time to support it. Note that this version no longer will create files
 *        for use with WiX 2.0. However, it will import and convert previously
 *        created Paraffin files to WiX 3.0.
 *        Yay! No more short filenames!
 *      - When adding files, I now check if they are .DLL, .EXE, or .OCX and if
 *        so, add CheckSum='yes' attribute to the File element.
 *      - All command line switches are now case insensitive. I'd forgotten to
 *        set that in a prior version.
 * 3.10 - Removed the code that converted from WiX 2.0 to WiX 3.0 Paraffin 
 *        files. It was limiting me from extending the code and it was only a 
 *        onetime use feature. I'll keep the 3.0 download link there for people
 *        who need it.
 *      - Added the -norootdirectory option which will not insert a <Directory>
 *        element on the root. All components in the root will be placed under 
 *        the <DirectoryRef> element.
 *      - Made the old -guids switch the default option so GUID values are 
 *        always assigned to components when first created.
 *      - Changed the way Paraffin creates component, directory, and file ID
 *        attributes. Now they all are assigned their own GUID. While this does
 *        make things harder to read, you're guaranteed to avoid all conflicts
 *        across larger projects. Also, it allows me to add a feature where you
 *        can include content into the Paraffin output. If you have .WXS files
 *        created with Paraffin 3.0, they are still properly updated with this
 *        version.
 *      - Added the ability to insert additional information into the output 
 *        file through .ParaffinMold files. All they are are .WXS files (for 
 *        ease of creating and editing) that under the <DirectoryRef> node 
 *        contain the additional values to insert into the current node. In 
 *        other words, if you put the the .ParaffinMold file in your .\Foo 
 *        directory. the data under the <DirectoryRef> in that file will be
 *        placed under the <Directory Id="Blah" Name=Foo"> element.
 *        Here's an example of a .ParaffinMold file:
 *        ------------------
 *         <?xml version="1.0" encoding="utf-8"?>
 *         <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
 *          <Fragment>
 *            <DirectoryRef Id="INSTALLDIR">
 *              <Component Id="HappyID" 
 *                            Guid="PUT-GUID_HERE">
 *                <RegistryKey Root="HKLM" 
 *                             Key="SOFTWARE\My Company\My Product" 
 *                             Action="createAndRemoveOnUninstall">
 *                  <RegistryValue Name="InstallRoot" Value="[INSTALDIR]" 
 *                                 Type="string" KeyPath="yes"/>
 *                </RegistryKey>
 *              </Component>
 *            </DirectoryRef>
 *          </Fragment>
 *        </Wix>
 *        ------------------
 *        Note that this feature works even if you are updating Paraffin files 
 *        created with version 3.0.
 * 3.11 - Fixed a bug where I wasn't properly handling the -dirref switch.
 * 3.12 - Fixed a bug where I wasn't putting .ParaffinMold files under the 
 *        Component node when the -multiple switch was used to create the file.
 *      - Added -DiskId switch to set the DiskId attribute for components.
 *      - Added -IncludeFile switch to include processing directive files in
 *        the output.
 *      - Deprecated the -Win64 switch.
 *      - Added the -Win64Var switch so you can specify values like 
 *        "$(var.yes64)" to allow conditional compilation between 32-bit and 
 *        64-bit installers.
 * 3.13 - Added the -regExExclude switch to allow better control over excluding
 *        files and directories. To maintain compatibility with existing 
 *        workflows, the regular expressions excludes are processed after -ext 
 *        and -direXclude switches. For files, the regular expression is applied
 *        to just the filename. For directories, the regular expression is 
 *        applied to the entire drive and path.
 *        This switch makes it much easier to run Paraffin on directories like
 *        a build output directory. To exclude the flotsam associated with a 
 *        normal VS build, you can add the following two switches to ensure you 
 *        get just the .EXE and .DLL files.
 *        -rex ".*\.vshost\.exe.*" -rex ".*codeanalysis.*"
 *      - The file creation date is no longer added to the file. This will make
 *        it easier for users who want to use a diff tool to compare updated
 *        files against the original file.
 * 3.50 - Refactored the code into three different files for ease of 
 *        development.
 *      - Added the -verbose command line option for verbose output.
 *      - Removed deprecated command line switches and the code reporting 
 *        their usage.
 *      - Paraffin no longer supports the -multiple switch that allowed multiple
 *        files per component. Multiple files per component breaks resiliency 
 *        and made my life more difficult doing upgrades. Please continue to 
 *        use Paraffin 3.13 if you need multiple files per component 
 *        support. From talking to the users of Paraffin, almost no one used 
 *        multiple files per component any more so I doubt this change caused 
 *        problems for anyone.
 *      - Paraffin now puts KeyPath='yes' attributes on File elements instead
 *        of Components. When updating an file created with an older version of
 *        Paraffin, the KeyPath is moved from the Component to the File. As this
 *        might be a breaking change, warning output shows the Component and 
 *        File elements affected.
 *      - Now the DiskId attribute is only added if the value is not the 
 *        default of 1.
 *      - Warnings are written in yellow text and errors are now written in red.
 *      - Added the -ReportIfDifferent switch. When updating, -ReportIfDifferent
 *        will make the program exit code 4 if the files are different. If the
 *        files are not different, the exit code is 0. If -ReportIfDifferent is
 *        not specified, the exit code will be 0. (Provided there's no other
 *        errors like invalid command lines, etc.)
 *      - Added the -PatchUpdate switch which when doing an update and finds a 
 *        file has been deleted, added the Transitive="Yes" attribute to the 
 *        component and adds a Conditional as a child. This allows minor updates
 *        to remove files.
 *      - Added the -PatchCreateFile switch. To do the WiX build so a minor
 *        upgrade can remove files, you need to have zero byte files for the 
 *        files you've deleted. Running with this switch will scan through the
 *        input file and create those files so your install build works. You can
 *        also specify this switch with the -PatchUpdate switch so as removed
 *        files are encountered, the zero byte files are created. Don't worry,
 *        Paraffin knows to not add those files once they have been deleted.
 * 3.60 - If you've added additional namespaces to the Paraffin produced .WXS
 *        file, which are required for your .ParaffinMold files to compile 
 *        correctly, those are now copied over to the .PARAFFIN file on updates.
 *        Because the standard xmlns="blah" attribute is not reported to LINQ to
 *        XML as an attribute, and all namespace attributes are placed before it
 *        when producing the file, you should put your custom namespace 
 *        declarations before xmlns="http://schemas.microsoft.com/wix/2006/wi"
 *        to make .WXS and .PARAFFIN file compares easier.
 * 3.61 - Fixed a bug where .PARAFFINMOLD files were included in the output 
 *        .WXS files when they should have been ignored.
 *      - Finally broke down and started using Resharper so carefully started
 *        fixing any warnings reported. Not all warnings are fixed, mainly the 
 *        ones that didn't destabilize the code.
 * 3.70 - Added support for WiX 4 fragments.
 *      - Updated to .NET 4.6.2.
 * 3.71 - Fixed a bug in WiX 4 support.
 -----------------------------------------------------------------------------*/
namespace Wintellect.Paraffin
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    /// <summary>
    /// The main entry point for the whole program. This also contains all 
    /// methods common to creating and updating files.
    /// </summary>
    internal static partial class Program
    {
        /// <summary>
        /// The file version we are dealing with.
        /// </summary>
        /// <remarks>
        /// This number increments every time there is new information
        /// written to the output file.
        /// Versions 0 and 1: Paraffin 3.0 and earlier.
        /// Version  2 : Paraffin 3.1
        /// </remarks>
        internal const Int32 CurrentFileVersion = 2;

        /// <summary>
        /// The file version for 1.0.
        /// </summary>
        private const Int32 Version1File = 1;

        #region Comment Options Elements
        // All the elements for the data stored in the comment.
        private const String VERSIONELEM = "ParaffinFileVersion";
        private const String CMDLINEOPTIONSELEM = "CommandLineOptions";
        private const String PRODUCEDBYELEM = "Producer";
        private const String WARNINGELEM = "WARNING";
        private const String DIRECTORYELEM = "Directory";
        private const String CUSTOMELEM = "Custom";
        private const String GROUPNAMEELEM = "GroupName";
        private const String ALIASELEM = "DirAlias";
        private const String INCREMENTELEM = "Increment";
        private const String MULTIPLEELEM = "Multiple";
        private const String NORECURSELEM = "Norecurse";
        private const String WIN64ELEM = "Win64";
        private const String EXTEXCLUDEELEM = "ExtensionExcludes";
        private const String EXTELEM = "Ext";
        private const String DIREEXCLUDEELEM = "DirExcludes";
        private const String DIREXT = "Dir";
        private const String NEXTDIRECTORYNUMELEM = "NextDirectoryNumber";
        private const String NEXTCOMPONENTNUMBER = "NextComponentNumber";
        private const String NODIRECTORYELEM = "NoRootDirectory";
        private const String DISKIDELEM = "DiskId";
        private const String INCLUDEFILESELEM = "IncludeFiles";
        private const String INCLUDEFILEITEMELEM = "File";
        private const String REGEXEXELEMENT = "RegExExcludes";
        private const String REGEXEXITEMELEM = "RegEx";
        private const String PERMANENT = "Permanent";
        private const String WIX4 = "WiX4";
        private const String PERUSER = "PerUser";
        #endregion

        // The PE file extensions.
        private static readonly String[] BinaryExtensions = { ".DLL", ".EXE", ".OCX" };

        // The argument values used across all the methods.
        private static ParaffinArgParser argValues;

        // The current directory number.
        private static Int32 directoryNumber;

        // The starting directory name. I use this to build up unique 
        // Directory Id values.
        private static String baseDirectoryName;

        // The full starting directory. If the user wants aliases, I'll replace
        // this with the alias.
        private static String fullStartDirectory;

        // The current component number
        private static Int32 componentNumber;

        // The error message.
        private static String errorMessage;

        // The TraceSource used for output.
        private static TraceSource verboseOut;

        /// <summary>
        /// The entry point for the application.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <returns>
        /// 0 - Successful
        /// 1 - Invalid command line argument.
        /// 2 - Invalid input file.
        /// 3 - File contains multiple files per component. No longer supported.
        /// 4 - If -ReportIfDifferent is specified on an update, a return value
        ///     of 4 indicates the input .WXS and output .PARAFFIN file are 
        ///     different.
        /// </returns>
        internal static Int32 Main(String[] args)
        {
            directoryNumber = 0;
            componentNumber = 0;
            errorMessage = String.Empty;

            verboseOut = new TraceSource(Constants.TraceSourceName,
                                         SourceLevels.Critical)
            {
                Switch = new SourceSwitch(Constants.SourceSwitchName)
            };

#if DEBUG==false
            // In release builds there's no sense doing the OutputDebugString
            // output since that causes a performance hit.
            verboseOut.Listeners.Remove("Default");
#endif

            // Think positive that everything will run completely.
            Int32 returnValue = 0;
            argValues = new ParaffinArgParser();
            if (args.Length > 0)
            {
                Boolean parsed = argValues.Parse(args);
                if (parsed)
                {
                    if (argValues.Verbose)
                    {
                        verboseOut.Switch.Level = SourceLevels.Information;
                        SmartConsoleTraceListener sctl =
                                                new SmartConsoleTraceListener();
                        verboseOut.Listeners.Add(sctl);
                    }

                    if (argValues.Update || argValues.PatchUpdate)
                    {
                        returnValue = UpdateExistingFile();
                    }
                    else if (argValues.PatchCreateFiles)
                    {
                        returnValue = CreateZeroByteFiles();
                    }
                    else
                    {
                        returnValue = CreateNewFile();
                    }
                }
            }
            else
            {
                argValues.OnUsage(String.Empty);
                returnValue = 1;
            }

            verboseOut.TraceInformation(Constants.VerboseReturnValue,
                                        returnValue);

            if (false == String.IsNullOrEmpty(errorMessage))
            {
                WriteError(errorMessage);
            }

            return returnValue;
        }

        /// <summary>
        /// Writes a error message to the screen.
        /// </summary>
        /// <param name="message">
        /// The message to report.
        /// </param>
        /// <param name="args">
        /// Any additional items to include in the output.
        /// </param>
        internal static void WriteError(String message, params Object[] args)
        {
            ColorWriteLine(ConsoleColor.Red, message, args);
        }

        /// <summary>
        /// Writes a warning message to the screen.
        /// </summary>
        /// <param name="message">
        /// The message to report.
        /// </param>
        /// <param name="args">
        /// Any additional items to include in the output.
        /// </param>
        private static void WriteWarning(String message, params Object[] args)
        {
            ColorWriteLine(ConsoleColor.Yellow, message, args);
        }

        /// <summary>
        /// For the <paramref name="directory"/> looks for any .ParaffinMold 
        /// files and adds their contents to the 
        /// <paramref cref="directoryElem"/> element.
        /// </summary>
        /// <param name="directory">
        /// The directory to get the files from.
        /// </param>
        /// <param name="directoryElem">
        /// The Director element to add the new .ParaffinMold file contents to.
        /// </param>
        private static void AddMoldFilesContentToNode(String directory,
                                                      XElement directoryElem)
        {
            // See if there are any mold files in this directory.
            String[] files = Directory.GetFiles(directory, "*.ParaffinMold");

            for (Int32 i = 0; i < files.Length; i++)
            {
                // Get the contents of the file and let any problems.
                XDocument inputMold = XDocument.Load(files[i]);

                // Get the nodes hanging off the DirectoryRef node.
                var toAddNodes = inputMold.Descendants(argValues.WixNamespace +
                                                       "DirectoryRef").Elements();

                Int32 count = toAddNodes.Count();
                Debug.Assert(count >= 1, "count >= 1");
                if (count >= 1)
                {
                    // Add the stuff the user defined.
                    directoryElem.Add(toAddNodes);
                }
                else
                {
                    String msg = String.Format(CultureInfo.CurrentCulture,
                                               Constants.InvalidMoldFile,
                                               files[i]);
                    throw new InvalidOperationException(msg);
                }
            }
        }

        /// <summary>
        /// Adds any include elements as the first element under the WiX 
        /// element.
        /// </summary>
        /// <param name="wixElement">
        /// The WiX element to add to.
        /// </param>
        private static void AddIncludeFiles(XElement wixElement)
        {
            foreach (var includeFile in argValues.IncludeFiles)
            {
                var include = new XProcessingInstruction("include",
                                                         includeFile);
                wixElement.AddFirst(include);
            }
        }

        /// <summary>
        /// Adds the command line options as the first comment element under
        /// the WiX element.
        /// </summary>
        /// <param name="wixElement">
        /// The WiX element to add to.
        /// </param>
        private static void AddCommandLineOptionsComment(XElement wixElement)
        {
            XElement initOptions =
                new XElement(CMDLINEOPTIONSELEM,
                    new XElement(PRODUCEDBYELEM, Constants.CommentProducer),
                    new XElement(WARNINGELEM, Constants.CommentWarning));
            if (argValues.Version > Version1File)
            {
                initOptions.Add(new XElement(VERSIONELEM, argValues.Version),
                             new XElement(GROUPNAMEELEM, argValues.GroupName));
            }
            else
            {
                initOptions.Add(
                    new XElement(CUSTOMELEM, argValues.GroupName),
                    new XElement(INCREMENTELEM, argValues.IncrementValue),
                    new XElement(NEXTDIRECTORYNUMELEM, directoryNumber),
                    new XElement(NEXTCOMPONENTNUMBER, componentNumber));
            }

            initOptions.Add(
                    new XElement(DIRECTORYELEM, argValues.StartDirectory),
                    new XElement(ALIASELEM, argValues.Alias),
                    new XElement(WIN64ELEM, argValues.Win64),
                    new XElement(NORECURSELEM, argValues.NoRecursion),
                    new XElement(NODIRECTORYELEM, argValues.NoRootDirectory),
                    new XElement(DISKIDELEM, argValues.DiskId),
                    new XElement(PERMANENT, argValues.Permanent),
                    new XElement(WIX4, argValues.WiX4),
                    new XElement(PERUSER, argValues.PerUser)
                    );

            // Add the file extension exclusions.
            XElement extList = new XElement(EXTEXCLUDEELEM);
            foreach (var item in argValues.ExtensionList)
            {
                extList.Add(new XElement(EXTELEM, item.Key));
            }

            initOptions.Add(extList);

            // Add the directory exclusions.
            XElement dirExList = new XElement(DIREEXCLUDEELEM);
            foreach (var item in argValues.DirectoryExcludeList)
            {
                dirExList.Add(new XElement(DIREXT, item));
            }

            initOptions.Add(dirExList);

            // Add the include file items.
            XElement includeList = new XElement(INCLUDEFILESELEM);
            foreach (var item in argValues.IncludeFiles)
            {
                includeList.Add(new XElement(INCLUDEFILEITEMELEM, item));
            }

            initOptions.Add(includeList);

            // Add the regular expression excludes.
            XElement regexList = new XElement(REGEXEXELEMENT);
            foreach (var item in argValues.RegExExcludes)
            {
                regexList.Add(new XElement(REGEXEXITEMELEM, item.ToString()));
            }

            initOptions.Add(regexList);

            // Add the XML comment.
            XComment comment = new XComment(initOptions.ToString());
            wixElement.AddFirst(comment);
        }

        /// <summary>
        /// Adds the ComponentGroup as the first child to the Fragment element.
        /// </summary>
        /// <param name="fragment">
        /// The Fragment element.
        /// </param>
        private static void AddComponentGroup(XElement fragment)
        {
            StringBuilder sb = new StringBuilder(70);
            if (argValues.Version == Version1File)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "group_{0}", argValues.GroupName);
            }
            else
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0}", argValues.GroupName);
            }

            // Grab all the Component elements and sort them by the ID 
            // attribute.
            var compNodes = from node in fragment.Descendants(argValues.WixNamespace + "Component")
                            select node;

            // In version 1 files I sorted the component elements so for old
            // files, continue to do that. For version 2 files and higher since
            // everything is a GUID it doesn't make sense.
            if (argValues.Version == Version1File)
            {
                compNodes = compNodes.OrderBy(
                                       n => n.Attribute("Id").Value,
                                       new LogicalStringComparer());
            }

            // Ensure that all invalid characters are stripped from the ID.
            String id = RemoveInvalidIdCharacters(sb.ToString());

            XElement groupNode = new XElement(argValues.WixNamespace + "ComponentGroup",
                                                new XAttribute("Id", id));
            foreach (var component in compNodes)
            {
                XAttribute attrib = new XAttribute("Id",
                                         component.Attribute("Id").Value);
                XElement refNode = new XElement(argValues.WixNamespace + "ComponentRef",
                                                attrib);
                groupNode.Add(refNode);
            }

            // Add the group node as the first child of the outputFragment only
            // if there are some components. It's perfectly reasonable to have
            // a fragment made up of nothing but directories.
            if (compNodes.Any())
            {
                fragment.AddFirst(groupNode);
            }
        }

        /// <summary>
        /// Gets the directory values initialized so the code can handle aliases
        /// and the individual directories.
        /// </summary>
        private static void InitializeDirectoryValues()
        {
            fullStartDirectory = Path.GetFullPath(argValues.StartDirectory);
            DirectoryInfo info = new DirectoryInfo(fullStartDirectory);
            baseDirectoryName = info.Name;
        }

        private static IEnumerable<String> ProcessedDirectoryFiles(
                                                              String directory)
        {
            IEnumerable<String> retValue = new List<String>();

            // Get the files in this directory.
            String[] files = Directory.GetFiles(directory);

            // Only do the work if there are some files in the directory.
            if (files.Length > 0)
            {
                // Skip all files whose extensions the user does not want,
                // skip .paraffinmold files, and skip files that are hidden.
                var validExtensions =
                    files.Where(f =>
                               {
                                   // same as extension list
                                   var ext = Path.GetExtension(f).ToUpperInvariant();
                                   if (null == ext)
                                   {
                                       return false;
                                   }

                                   return (argValues.ExtensionList.ContainsKey(ext) == false) &&
                                           (String.Compare(".PARAFFINMOLD",
                                                           ext,
                                                           StringComparison.CurrentCultureIgnoreCase) != 0) &&
                                           ((File.GetAttributes(f) & FileAttributes.Hidden) != FileAttributes.Hidden);
                               });

                // Skip all those filenames that might match the regex.
                var noRegExMatch = validExtensions.Where(f => argValues.RegExExcludes
                                                                       .Find(m => m.IsMatch(f)) == null);
                retValue = noRegExMatch;
            }

            return retValue;
        }

        /// <summary>
        /// MSI only accepts IDs that are 72 characters long so I need to ensure
        /// that the strings I use are within that limit.
        /// </summary>
        /// <param name="start">
        /// The initial part of the string.
        /// </param>
        /// <param name="main">
        /// The main part of the string.
        /// </param>
        /// <param name="uniqueId">
        /// The unique value to append to this string.
        /// </param>
        /// <returns>
        /// A unique string that is only 70 characters long and has all invalid
        /// characters stripped.
        /// </returns>
        private static String CreateSeventyCharIdString(String start,
                                                        String main,
Int32 uniqueId)
        {
            const String FormatStr = "{0}_{1}_{2}";
            const Int32 MaxLen = 70;

            String uniqueStr = String.Format(CultureInfo.InvariantCulture,
                                             "{0}",
                                             uniqueId);
            StringBuilder sb = new StringBuilder(100);
            sb.AppendFormat(CultureInfo.InvariantCulture, FormatStr, start, main, uniqueStr);
            if (sb.Length > MaxLen)
            {
                sb.Length = 0;
                Int32 idLen = uniqueStr.Length;
                Int32 startLen = start.Length;
                Int32 len = Math.Min(main.Length, MaxLen - (idLen + startLen));
                String sub = main.Substring(0, len);
                sb.AppendFormat(CultureInfo.InvariantCulture, FormatStr, start, sub, uniqueStr);
            }

            // Turns out id strings in WiX cannot have dashes in them so 
            // convert them to underscores.
            String retVal = RemoveInvalidIdCharacters(sb.ToString());
            return retVal;
        }

        /// <summary>
        /// Looks through the list of directory exclusions and returns true
        /// if this directory is supposed to be excluded.
        /// </summary>
        /// <param name="directory">
        /// The directory to check if it's got any excluded value in it.
        /// </param>
        /// <returns>
        /// True  - Supposed to exclude and skip.
        /// False - Process this directory.
        /// </returns>
        private static Boolean IsDirectoryExcluded(String directory)
        {
            // If the user wanted to skip some directories, check to see
            // if this happens to be one.
            for (Int32 i = 0; i < argValues.DirectoryExcludeList.Count; i++)
            {
                if (directory.Contains(argValues.DirectoryExcludeList[i]))
                {
                    // Return now so we don't do the regex checks.
                    return true;
                }
            }

            // Look at the regular expressions to skip as well.
            for (Int32 i = 0; i < argValues.RegExExcludes.Count; i++)
            {
                if (argValues.RegExExcludes[i].IsMatch(directory))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a unique name for the Directory Id attribute.
        /// </summary>
        /// <param name="directory">
        /// The full directory to process.
        /// </param>
        /// <returns>
        /// A string that encapsulates the directory name with a unique value
        /// appended.
        /// </returns>
        private static String GenerateUniqueDirectoryIdName(String directory)
        {
            if (argValues.Version == Version1File)
            {
                // To make the Id a bit easier to read, I'm going to use a 
                // naming scheme of "dir_<Path>_<Num>". That will put some 
                // uniqueness on the names so they don't conflict across large 
                // installs.

                // Suffix the directory with \ to ensure I find the exact match 
                // when looking the base directory.
                if (false == directory.EndsWith("\\",
                                            StringComparison.OrdinalIgnoreCase))
                {
                    directory += "\\";
                }

                // Figure out where the base directory name is in this string 
                // and create a unique name for the Id attribute.
                String exactBaseDirectory =
                            String.Format(CultureInfo.InvariantCulture,
                                            "\\{0}\\",
                                            baseDirectoryName);
                Int32 startBaseDir = directory.IndexOf(exactBaseDirectory,
                                           StringComparison.OrdinalIgnoreCase);

                // Get the real value and skip the preceding \\ used to find the
                // exact base.
                String dirIdString = directory.Substring(startBaseDir + 1);
                if ('\\' == dirIdString[dirIdString.Length - 1])
                {
                    dirIdString = dirIdString.Substring(0,
                                                       dirIdString.Length - 1);
                }

                dirIdString = dirIdString.Replace('\\', '.');
                dirIdString = CreateSeventyCharIdString("dir",
                                                        dirIdString,
                                                        directoryNumber);

                // Since I've used this directoryNumber, time to bump it up.
                directoryNumber += argValues.IncrementValue;

                return dirIdString;
            }

            Guid g = Guid.NewGuid();
            String guidString = g.ToString("N").ToUpperInvariant();
            return String.Format(CultureInfo.InvariantCulture,
                                    "dir_{0}",
                                    guidString);
        }

        /// <summary>
        /// Creates a new Directory element.
        /// </summary>
        /// <param name="directory">
        /// The file directory for this element.
        /// </param>
        /// <returns>
        /// A constructed <see cref="XElement"/>.
        /// </returns>
        private static XElement CreateDirectoryElement(String directory)
        {
            // Each directory element needs a unique value.
            String uniqueDirId = GenerateUniqueDirectoryIdName(directory);

            // Get the long and short names for this directory.
            DirectoryInfo info = new DirectoryInfo(directory);

            // I've got enough to create the Directory node.
            XElement directoryNode = new XElement(argValues.WixNamespace + "Directory",
                                                  new XAttribute("Id", uniqueDirId),
                                                  new XAttribute("Name", info.Name));
            return directoryNode;
        }

        /// <summary>
        /// Creates a File element for the file in <paramref name="fileName"/>.
        /// </summary>
        /// <param name="fileName">
        /// The full filename to process.
        /// </param>
        /// <returns>
        /// A valid <see cref="XElement"/> for the File element.
        /// </returns>
        private static XElement CreateFileElement(String fileName)
        {
            String fileId;
            if (argValues.Version == Version1File)
            {
                // Create a unique filename. In a one file per component run, 
                // this will mean that the file and it's parent component will 
                // have the same number.
                fileId = CreateSeventyCharIdString("file",
                                                   argValues.GroupName,
                                                   componentNumber - 1);
            }
            else
            {
                Guid g = Guid.NewGuid();
                fileId = String.Format(CultureInfo.InvariantCulture,
                                       "file_{0}",
                         g.ToString("N").ToUpper(CultureInfo.InvariantCulture));
            }

            XElement file = new XElement(argValues.WixNamespace + "File",
                                         new XAttribute("Id", fileId));
            if (IsPortableEExecutableFile(fileName))
            {
                file.Add(new XAttribute("Checksum", "yes"));
            }

            if (!argValues.PerUser)
            {
                file.Add(new XAttribute("KeyPath", "yes"));
            }

            fileName = AliasedFilename(fileName);
            file.Add(new XAttribute("Source", fileName));
            return file;
        }

        /// <summary>
        /// Creates a RegistryValue element for the file in <paramref name="fileName"/>.
        /// This is to facilitate LimitedPrivilige/PerUser MSI creation
        /// </summary>
        /// <param name="fileName">
        /// The full filename to process.
        /// </param>
        /// <returns>
        /// A valid <see cref="XElement"/> for the RegistryValue element.
        /// </returns>
        private static XElement CreateRegistryValueElement(String fileName)
        {
            String fileId;
            if (argValues.Version == Version1File)
            {
                // Create a unique filename. In a one file per component run, 
                // this will mean that the file and it's parent component will 
                // have the same number.
                fileId = CreateSeventyCharIdString("file",
                                                   argValues.GroupName,
                                                   componentNumber - 1);
            }
            else
            {
                Guid g = Guid.NewGuid();
                fileId = String.Format(CultureInfo.InvariantCulture,
                                       "file_{0}",
                         g.ToString("N").ToUpper(CultureInfo.InvariantCulture));
            }

            XElement registryValue = new XElement(argValues.WixNamespace + "RegistryValue");
            registryValue.Add(new XAttribute("Root", "HKCU"));
            registryValue.Add(new XAttribute("Key", "Software\\Tolt Technologies\\Ability Drive\\InstalledFiles"));
            registryValue.Add(new XAttribute("Name", fileId));
            registryValue.Add(new XAttribute("Value", ""));
            registryValue.Add(new XAttribute("Type", "string"));
            registryValue.Add(new XAttribute("KeyPath", "yes"));

            return registryValue;
        }

        /// <summary>
        /// Creates a standard Component element.
        /// </summary>
        /// <returns>
        /// A newly created and unique Component elements.
        /// </returns>
        private static XElement CreateComponentElement()
        {
            String componentId;
            if (argValues.Version == Version1File)
            {
                // Make sure the Id field is less than or equal to 70 
                // characters.
                componentId = CreateSeventyCharIdString("comp",
                                                        argValues.GroupName,
                                                        componentNumber);

                // Increment since I just used that number.
                componentNumber++;
            }
            else
            {
                Guid g = Guid.NewGuid();
                componentId = String.Format(CultureInfo.InvariantCulture,
                                            "comp_{0}",
                 g.ToString("N").ToUpper(CultureInfo.InvariantCulture));
            }

            String guidString = Guid.NewGuid().ToString().ToUpperInvariant();


            XElement comp = new XElement(argValues.WixNamespace + "Component",
                                         new XAttribute("Id", componentId),
                                         new XAttribute("Guid", guidString));

            if (argValues.DiskId > 1)
            {
                comp.Add(new XAttribute("DiskId", argValues.DiskId));
            }

            if (argValues.Permanent == true)
            {
                comp.Add(new XAttribute("Permanent", "yes"));
            }

            // Does the user want the Win64 attribute?
            if (false == String.IsNullOrEmpty(argValues.Win64))
            {
                comp.SetAttributeValue("Win64", argValues.Win64);
            }

            return comp;
        }

        private static String AliasedFilename(String fileName)
        {
            // Does the user want an alias for the base directory name?
            if (false == String.IsNullOrEmpty(argValues.Alias))
            {
                fileName = fileName.Replace(fullStartDirectory,
                                            argValues.Alias);
            }

            return fileName;
        }

        private static String UnAliasedFilename(String fileName)
        {
            if (false == String.IsNullOrEmpty(argValues.Alias))
            {
                fileName = fileName.Replace(argValues.Alias,
                                            fullStartDirectory);
            }

            return fileName;
        }

        private static Boolean IsPortableEExecutableFile(String fileName)
        {
            String ext = Path.GetExtension(fileName);
            if (null != ext)
            {
                ext = ext.ToUpper(CultureInfo.CurrentCulture);

                for (Int32 i = 0; i < BinaryExtensions.Length; i++)
                {
                    if (0 == String.Compare(ext,
                                            BinaryExtensions[i],
                                            true,
                                            CultureInfo.CurrentCulture))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static String RemoveInvalidIdCharacters(String input)
        {
            // WiX 3.0 does not document the actual valid characters in an Id
            // attribute. This is especially true in Directory elements. What
            // I'll do here is replace everything that's not in the range
            // [0-9a-zA-Z_] with underscores. While that might make some of the
            // Ids harder to read, I'm assured CANDLE.EXE will compile the
            // fragment.
            return Regex.Replace(input, "[^0-9a-zA-Z_]", "_");
        }

        private static void ColorWriteLine(ConsoleColor color,
                                           String message,
                                           params Object[] args)
        {
            ConsoleColor currForeground = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message, args);
            }
            finally
            {
                Console.ForegroundColor = currForeground;
            }
        }

        /// <summary>
        /// Wrapper class for the native windows APIs used by this program.
        /// </summary>
        private static class NativeMethods
        {
            [DllImport("shlwapi.dll",
                       CharSet = CharSet.Unicode,
                       ExactSpelling = true)]
            internal static extern Int32 StrCmpLogicalW(String x, String y);
        }

        /// <summary>
        ///  Used to sort values in strings in logical order.
        /// </summary>
        private sealed class LogicalStringComparer : IComparer<String>
        {
            /// <summary>
            /// Calls the native logical string compare method.
            /// </summary>
            /// <param name="x">
            /// The first string to compare.
            /// </param>
            /// <param name="y">
            /// The second string to compare.
            /// </param>
            /// <returns>
            /// Zero if the strings are identical, 1 if 
            /// <paramref name="x"/> is greater, -1 if 
            /// <paramref name="y"/> is greater.
            /// </returns>
            public Int32 Compare(String x, String y)
            {
                return NativeMethods.StrCmpLogicalW(x, y);
            }
        }
    }
}
