//------------------------------------------------------------------------------
// <copyright file="ParaffinArgParser.cs" company="Wintellect">
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
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    /// <summary>
    /// Implements the command line parsing for the Paraffin program.
    /// </summary>
    internal class ParaffinArgParser : ArgParser
    {
        #region Command Line Option Constants
        private const String ALIAS = "alias";
        private const String ALIASSHORT = "a";
        private const String DIR = "dir";
        private const String DIREXCLUDE = "direXclude";
        private const String DIREXCLUDESHORT = "x";
        private const String DIRREF = "dirref";
        private const String DIRREFSHORT = "dr";
        private const String DIRSHORT = "d";
        private const String DISKID = "diskid";
        private const String DISKIDSHORT = "did";
        private const String EXT = "ext";
        private const String EXTSHORT = "e";
        private const String GROUPNAME = "groupname";
        private const String GROUPNAMESHORT = "gn";
        private const String HELP = "help";
        private const String HELPQUESTION = "?";
        private const String HELPSHORT = "h";
        private const String INCFILE = "includeFile";
        private const String INCFILESHORT = "if";
        private const String NORECURSE = "norecurse";
        private const String NORECURSESHORT = "nr";
        private const String NOROOTDIRECTORY = "norootdirectory";
        private const String NOROOTDIRECTORYSHORT = "nrd";
        private const String PATCHCREATEFILES = "PatchCreateFiles";
        private const String PATCHCREATEFILESSHORT = "pcf";
        private const String PATCHUPDATE = "PatchUpdate";
        private const String PATCHUPDATESHORT = "pu";
        private const String REGEXEXCLUDE = "regExExclude";
        private const String REGEXEXCLUDESHORT = "rex";
        private const String REPORTIFDIFFERENT = "ReportIfDifferent";
        private const String REPORTIFDIFFERENTSHORT = "rid";
        private const String UPDATE = "update";
        private const String UPDATESHORT = "u";
        private const String VERBOSE = "verbose";
        private const String VERBOSESHORT = "v";
        private const String WIN64VAR = "win64var";
        private const String PERMANENT = "permanent";
        private const String WIX4 = "WiX4";
        private const String PERUSER = "perUser";
        #endregion

        private const String DEFAULTDIRREF = "INSTALLDIR";

        // The private string to hold more detailed error information.
        private String errorMessage;

        // Indicates the error was found in OnDoneParse.
        private Boolean errorInOnDoneParse;

        // Flag for indicating WiX 4.
        private Boolean useWiX4;

        /// <summary>
        /// Initializes a new instance of the ParaffinArgParser class.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.SpacingRules",
                         "SA1026:CodeMustNotContainSpaceAfterNewKeywordInImplicitlyTypedArrayAllocation",
                         Justification = "Much easier to read this way."),
         SuppressMessage("StyleCop.CSharp.ReadabilityRules",
                         "SA1118:ParameterMustNotSpanMultipleLines",
                         Justification = "Much easier to read this way.")]
        public ParaffinArgParser()
            : base(new[]
                        {
                            HELP,
                            HELPQUESTION,
                            HELPSHORT,
                            NORECURSE,
                            NORECURSESHORT,
                            NOROOTDIRECTORY,
                            NOROOTDIRECTORYSHORT,
                            PATCHUPDATE,
                            PATCHUPDATESHORT,
                            PATCHCREATEFILES,
                            PATCHCREATEFILESSHORT,
                            REPORTIFDIFFERENT,
                            REPORTIFDIFFERENTSHORT,
                            UPDATE,
                            UPDATESHORT,
                            VERBOSE,
                            VERBOSESHORT,
                            PERMANENT,
                            WIX4,
                            PERUSER
                        },
                  new[]
                        {
                            ALIAS,
                            ALIASSHORT,
                            DIR,
                            DIREXCLUDE,
                            DIREXCLUDESHORT,
                            DIRREF,
                            DIRREFSHORT,
                            DIRSHORT,
                            DISKID,
                            DISKIDSHORT,
                            EXT,
                            EXTSHORT,
                            GROUPNAME,
                            GROUPNAMESHORT,
                            INCFILE,
                            INCFILESHORT,
                            REGEXEXCLUDE,
                            REGEXEXCLUDESHORT,
                            WIN64VAR
                        },
                    true)
        {
            // Set all the appropriate defaults.
            this.FileName = String.Empty;
            this.StartDirectory = String.Empty;
            this.GroupName = String.Empty;
            this.Alias = String.Empty;
            this.NoRootDirectory = false;
            this.NoRootDirectoryState = false;
            this.DirectoryRef = String.Empty;
            this.ExtensionList = new Dictionary<String, Boolean>();
            this.IncrementValue = 1;
            this.DirectoryExcludeList = new List<String>();
            this.DiskId = 1;
            this.IncludeFiles = new List<String>();
            this.RegExExcludes = new List<Regex>();
            this.Win64 = String.Empty;
            this.Permanent = false;
            this.WiX4 = false;
            this.PerUser = false;

            this.Version = Program.CurrentFileVersion;

            this.errorMessage = String.Empty;
        }

        /// <summary>
        /// Gets or sets the version of the input file when updating.
        /// </summary>
        public Int32 Version { get; set; }

        /// <summary>
        /// Gets or sets the output filename.
        /// </summary>
        public String FileName { get; set; }

        #region Required Creation Parameters
        /// <summary>
        /// Gets or sets the directory to process.
        /// </summary>
        public String StartDirectory { get; set; }

        /// <summary>
        /// Gets or sets the component group name.
        /// </summary>
        public String GroupName { get; set; }

        #endregion

        #region Optional Creation Parameters
        /// <summary>
        /// Gets the DirectoryRef Id if you want the fragment files to go 
        /// somewhere else besides the INSTALLDIR.
        /// </summary> 
        public String DirectoryRef { get; private set; }

        /// <summary>
        /// Gets or sets the DiskId value applied to each component. The default
        /// is 1.
        /// </summary>
        public Int32 DiskId { get; set; }

        /// <summary>
        /// Gets or sets the value to replace the starting directory in the File 
        /// element src attribute.
        /// </summary>
        public String Alias { get; set; }

        /// <summary>
        /// Gets the list of extensions to skip.
        /// </summary>
        public Dictionary<String, Boolean> ExtensionList { get; private set; }

        /// <summary>
        /// Gets or sets the amount to add to each component number to leave 
        /// room for additional component files between directories.
        /// </summary>
        public Int32 IncrementValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user does not want to 
        /// recurse the directories.
        /// </summary>
        public Boolean NoRecursion { get; set; }

        /// <summary>
        /// Gets the list of directories to exclude from the processing.
        /// </summary>
        public List<String> DirectoryExcludeList { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the Win64 attribute is 
        /// added to components.
        /// </summary>
        public String Win64 { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user does not want the
        /// root &lt;Directory&gt; element included.
        /// </summary>
        public Boolean NoRootDirectory { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the current state of the no 
        /// root option so the code can know if it is processing the root 
        /// directory or a recursed child directory.
        /// </summary>
        public Boolean NoRootDirectoryState { get; set; }

        /// <summary>
        /// Gets the list of include files to be included into the output .WXS
        /// file. Note that there's no checking on existence or validity of 
        /// these values.
        /// </summary>
        public List<String> IncludeFiles { get; private set; }

        /// <summary>
        /// Gets whether the files are supposed to be permanently installed
        /// </summary>
        public Boolean Permanent { get; set; }

        /// <summary>
        /// Gets whether the user wants the WiX 4 namespace or not. 
        /// Defaults to false.
        /// </summary>
        public Boolean WiX4
        {
            get => this.useWiX4;
            set
            {
                if (value == true)
                {
                    this.WixNamespace = "http://wixtoolset.org/schemas/v4/wxs";
                    this.useWiX4 = true;
                }
                else
                {
                    this.WixNamespace = "http://schemas.microsoft.com/wix/2006/wi";
                    this.useWiX4 = false;
                }
            }
        }

        /// <summary>
        /// Gets whether the generated file components should include a registry key
        /// to support perUser limitedPrivileges MSIs
        /// </summary>
        public Boolean PerUser { get; set; }

        /// <summary>
        /// Gets the namespace as this is different between WiX 3 and WiX 4.
        /// Defaults to WiX3.
        /// </summary>
        public XNamespace WixNamespace { get; private set; }

        #endregion

        /// <summary>
        /// Gets the list of regular expression excludes that are applied to 
        /// files and directories.
        /// </summary>
        public List<Regex> RegExExcludes { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the user wants to update a 
        /// previously created file.
        /// </summary>
        public Boolean Update { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the users wants verbose
        /// processing output or not.
        /// </summary>
        public Boolean Verbose { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user wants to compare 
        /// the input .WXS and output .PARAFFIN files and if different, report
        /// this through an exit code of 4.
        /// </summary>
        public Boolean ReportIfDifferent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user wants the .PARAFFIN
        /// file to have the transitive patch information put in the file.
        /// </summary>
        public Boolean PatchUpdate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the users wants to create
        /// the zero byte files for all removed components so your minor updates
        /// properly remove the files.
        /// </summary>
        public Boolean PatchCreateFiles { get; set; }

        /// <summary>
        /// Reports correct command line usage.
        /// </summary>
        /// <param name="errorInfo">
        /// The string with the invalid command line option.
        /// </param>
        public override void OnUsage(String errorInfo)
        {
            ProcessModule exe = Process.GetCurrentProcess().Modules[0];
            Console.WriteLine(Constants.UsageString,
                                            exe.FileVersionInfo.FileVersion);
            if ((false == this.errorInOnDoneParse) &&
                 (false == String.IsNullOrEmpty(errorInfo)))
            {
                Console.WriteLine();
                Program.WriteError(Constants.ErrorSwitch, errorInfo);
            }

            if (false == String.IsNullOrEmpty(this.errorMessage))
            {
                Console.WriteLine();
                Program.WriteError(this.errorMessage);
            }
        }

        /// <summary>
        /// Called when a switch is parsed out.
        /// </summary>
        /// <param name="switchSymbol">
        /// The switch value parsed out.
        /// </param>
        /// <param name="switchValue">
        /// The value of the switch. For flag switches this is null/Nothing.
        /// </param>
        /// <returns>
        /// One of the <see cref="ArgParser.SwitchStatus"/> values.
        /// </returns>
        [SuppressMessage("Microsoft.Design",
                         "CA1062:Validate arguments of public methods",
                         MessageId = "1",
                         Justification =
              "This method can never be called with an invalid switchvalue " +
              "so there is no need to validate it here."),
        SuppressMessage("Microsoft.Maintainability",
                         "CA1502:AvoidExcessiveComplexity",
                         Justification =
              "A switch statement using strings always generates complexity.")]
        protected override SwitchStatus OnSwitch(String switchSymbol,
String switchValue)
        {
            SwitchStatus ss = SwitchStatus.NoError;
            switch (switchSymbol)
            {
                case ALIASSHORT:
                case ALIAS:
                    if (false == String.IsNullOrEmpty(this.Alias))
                    {
                        this.errorMessage = Constants.AliasMultipleSwitches;
                        ss = SwitchStatus.Error;
                    }
                    else
                    {
                        // If the alias does not end with a \, add one to 
                        // help the user out.
                        if (false == switchValue.EndsWith("\\",
                                        StringComparison.OrdinalIgnoreCase))
                        {
                            switchValue += "\\";
                        }

                        this.Alias = switchValue;
                    }

                    break;

                case DIRSHORT:
                case DIR:
                    if (false == String.IsNullOrEmpty(this.StartDirectory))
                    {
                        this.errorMessage = Constants.DirectoryMultipleSwitches;
                        ss = SwitchStatus.Error;
                    }
                    else
                    {
                        // If the directory does not end with a \, add one.
                        if (false == switchValue.EndsWith("\\",
                                          StringComparison.OrdinalIgnoreCase))
                        {
                            switchValue += "\\";
                        }

                        this.StartDirectory = switchValue;
                    }

                    break;

                case DIREXCLUDESHORT:
                case DIREXCLUDE:
                    this.DirectoryExcludeList.Add(switchValue);
                    break;

                case DIRREFSHORT:
                case DIRREF:
                    if (false == String.IsNullOrEmpty(this.DirectoryRef))
                    {
                        this.errorMessage =
                                         Constants.DirectoryRefMultipleSwitches;
                        ss = SwitchStatus.Error;
                    }
                    else
                    {
                        this.DirectoryRef = switchValue;
                    }

                    break;

                case DISKID:
                case DISKIDSHORT:
                    {
                        // Only integer values are acceptable.
                        if (false == Int32.TryParse(switchValue, out Int32 outVal))
                        {
                            this.errorMessage = Constants.DiskIdMustBeInteger;
                            ss = SwitchStatus.Error;
                        }
                        else
                        {
                            this.DiskId = outVal;
                        }
                    }

                    break;

                case EXTSHORT:
                case EXT:
                    {
                        // Does it start with a period? If not, add one to help
                        // the user out.
                        if ('.' != switchValue[0])
                        {
                            switchValue = "." + switchValue;
                        }

                        // You can have as many -ext switches as you want.
                        this.ExtensionList.Add(switchValue.ToUpperInvariant(),
                                               true);
                    }

                    break;

                case GROUPNAMESHORT:
                case GROUPNAME:
                    ss = this.ProcessGroupName(switchValue);
                    break;

                case HELPQUESTION:
                case HELPSHORT:
                case HELP:
                    ss = SwitchStatus.ShowUsage;
                    break;

                case INCFILE:
                case INCFILESHORT:
                    this.IncludeFiles.Add(switchValue);
                    break;

                case NORECURSESHORT:
                case NORECURSE:
                    this.NoRecursion = true;
                    break;

                case NOROOTDIRECTORY:
                case NOROOTDIRECTORYSHORT:
                    this.NoRootDirectoryState = true;
                    this.NoRootDirectory = true;
                    break;

                case PATCHUPDATE:
                case PATCHUPDATESHORT:
                    this.PatchUpdate = true;
                    break;

                case PATCHCREATEFILES:
                case PATCHCREATEFILESSHORT:
                    this.PatchCreateFiles = true;
                    break;

                case REGEXEXCLUDE:
                case REGEXEXCLUDESHORT:
                    {
                        // Do the regular expression conversion here so any 
                        // errors are reported before I start parsing.
                        try
                        {
                            Regex newRegex = new Regex(switchValue,
                                                       RegexOptions.IgnoreCase);
                            this.RegExExcludes.Add(newRegex);
                        }
                        catch (ArgumentException ex)
                        {
                            // There's a problem with the regular expression.
                            ss = SwitchStatus.Error;
                            this.errorMessage = ex.Message;
                        }
                    }

                    break;

                case REPORTIFDIFFERENT:
                case REPORTIFDIFFERENTSHORT:
                    this.ReportIfDifferent = true;
                    break;

                case UPDATESHORT:
                case UPDATE:
                    this.Update = true;
                    break;

                case VERBOSESHORT:
                case VERBOSE:
                    this.Verbose = true;
                    break;

                case WIN64VAR:
                    if (false == String.IsNullOrEmpty(this.Win64))
                    {
                        this.errorMessage = Constants.Win64VarMultipleSwitches;
                        ss = SwitchStatus.Error;
                    }
                    else
                    {
                        this.Win64 = switchValue;
                    }

                    break;

                case PERMANENT:
                    this.Permanent = true;
                    break;

                case WIX4:
                    this.WiX4 = true;
                    break;

                case PERUSER:
                    this.PerUser = true;
                    break;

                default:
                    {
                        this.errorMessage = Constants.UnknownCommandLineOption;
                        ss = SwitchStatus.Error;
                    }

                    break;
            }

            return ss;
        }

        /// <summary>
        /// Called when a non-switch value is parsed out.
        /// </summary>
        /// <param name="value">
        /// The value parsed out.
        /// </param>
        /// <returns>
        /// One of the <see cref="ArgParser.SwitchStatus"/> values.
        /// </returns>
        protected override SwitchStatus OnNonSwitch(String value)
        {
            SwitchStatus ss = SwitchStatus.NoError;
            if (false == String.IsNullOrEmpty(this.FileName))
            {
                this.errorMessage = Constants.OutputAlreadySpecified;
                ss = SwitchStatus.Error;
            }
            else if (String.IsNullOrEmpty(value))
            {
                this.errorMessage = Constants.OutputCannotBeEmpty;
                ss = SwitchStatus.Error;
            }
            else
            {
                this.FileName = value;
            }

            // There are no non switches allowed.
            this.errorMessage = Constants.UnknownCommandLineOption;
            return ss;
        }

        /// <summary>
        /// Called when parsing is finished so final sanity checking can be
        /// performed.
        /// </summary>
        /// <returns>
        /// One of the <see cref="ArgParser.SwitchStatus"/> values.
        /// </returns>
        protected override SwitchStatus OnDoneParse()
        {
            SwitchStatus ss = SwitchStatus.NoError;

            // The output file can never be null.
            if (String.IsNullOrEmpty(this.FileName))
            {
                this.errorMessage = Constants.OutputCannotBeEmpty;
                ss = SwitchStatus.Error;
                this.errorInOnDoneParse = true;
            }

            if ((false == this.Update) &&
                (false == this.PatchUpdate) &&
                (false == this.PatchCreateFiles))
            {
                // Check that I at least have a directory and prefix. Everything
                // else is optional when creating files.
                if (String.IsNullOrEmpty(this.StartDirectory))
                {
                    this.errorMessage = Constants.DirectoryCannotBeEmpty;
                    ss = SwitchStatus.Error;
                    this.errorInOnDoneParse = true;
                }
                else if (false == Directory.Exists(this.StartDirectory))
                {
                    this.errorMessage = Constants.DirectoryDoesNotExist;
                    ss = SwitchStatus.Error;
                    this.errorInOnDoneParse = true;
                }
                else if (String.IsNullOrEmpty(this.GroupName))
                {
                    this.errorMessage = Constants.GroupNameCannotBeEmpty;
                    ss = SwitchStatus.Error;
                    this.errorInOnDoneParse = true;
                }
                else if (this.PatchCreateFiles || this.PatchUpdate)
                {
                    this.errorMessage = Constants.NoPatchWhenCreating;
                    ss = SwitchStatus.Error;
                    this.errorInOnDoneParse = true;
                }

                // If no directory ref was specified, set it to the default.
                if (String.IsNullOrEmpty(this.DirectoryRef))
                {
                    this.DirectoryRef = DEFAULTDIRREF;
                }
            }
            else if (this.Update)
            {
                // The user is asking to update.
                // Check that they didn't also specify creation options.
                if (false == String.IsNullOrEmpty(this.StartDirectory) ||
                     (false == String.IsNullOrEmpty(this.GroupName)))
                {
                    this.errorMessage = Constants.MutuallyExclusiveOptions;
                    ss = SwitchStatus.Error;
                    this.errorInOnDoneParse = true;
                }

                // Check to see if the user asked for patch updating, too.
                if (this.PatchUpdate)
                {
                    this.errorMessage = Constants.MutuallyExclusiveOptions;
                    ss = SwitchStatus.Error;
                    this.errorInOnDoneParse = true;
                }
            }
            else if (this.PatchUpdate)
            {
                // Check that they didn't also specify creation options.
                if (false == String.IsNullOrEmpty(this.StartDirectory) ||
                     (false == String.IsNullOrEmpty(this.GroupName)))
                {
                    this.errorMessage = Constants.MutuallyExclusiveOptions;
                    ss = SwitchStatus.Error;
                    this.errorInOnDoneParse = true;
                }

                // Check to at least see if the file exists.
                if (false == File.Exists(this.FileName))
                {
                    this.errorMessage = Constants.UpdateFileMustExist;
                    ss = SwitchStatus.Error;
                    this.errorInOnDoneParse = true;
                }
            }

            return ss;
        }

        /// <summary>
        /// Handles processing the -groupname switch.
        /// </summary>
        /// <param name="switchValue">
        /// The value of the -groupname switch.
        /// </param>
        /// <returns>
        /// One of the <see cref="ArgParser.SwitchStatus"/> values.
        /// </returns>
        private SwitchStatus ProcessGroupName(String switchValue)
        {
            SwitchStatus ss = SwitchStatus.NoError;
            if (false == String.IsNullOrEmpty(this.GroupName))
            {
                this.errorMessage = Constants.GroupNameMultipleSwitches;
                ss = SwitchStatus.Error;
            }
            else if (String.IsNullOrEmpty(switchValue))
            {
                this.errorMessage = Constants.GroupNameCannotBeEmpty;
                ss = SwitchStatus.Error;
            }
            else if (switchValue.Length >= 65)
            {
                this.errorMessage = Constants.GroupNameTooLong;
                ss = SwitchStatus.Error;
            }
            else
            {
                this.GroupName = switchValue;
            }

            return ss;
        }
    }
}
