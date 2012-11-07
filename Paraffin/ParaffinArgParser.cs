﻿//------------------------------------------------------------------------------
// <copyright file="ParaffinArgParser.cs" company="Wintellect">
//    Copyright (c) 2002-2010 John Robbins/Wintellect -- All rights reserved.
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

    /// <summary>
    /// Implements the command line parsing for the Paraffin program.
    /// </summary>
    internal class ParaffinArgParser : ArgParser
    {
        #region Command Line Option Constants
        private const string ALIAS = "alias";
        private const string ALIASSHORT = "a";
        private const string CUSTOM = "custom";
        private const string CUSTOMSHORT = "c";
        private const string DIR = "dir";
        private const string DIREXCLUDE = "direXclude";
        private const string DIREXCLUDESHORT = "x";
        private const string DIRREF = "dirref";
        private const string DIRREFSHORT = "dr";
        private const string DIRSHORT = "d";
        private const string DISKID = "diskid";
        private const string DISKIDSHORT = "did";
        private const string EXT = "ext";
        private const string EXTSHORT = "e";
        private const string GROUPNAME = "groupname";
        private const string GROUPNAMESHORT = "gn";
        private const string GUIDS = "guids";
        private const string GUIDSSHORT = "g";
        private const string HELP = "help";
        private const string HELPQUESTION = "?";
        private const string HELPSHORT = "h";
        private const string INC = "inc";
        private const string INCFILE = "includeFile";
        private const string INCFILESHORT = "if";
        private const string INCSHORT = "i";
        private const string NORECURSE = "norecurse";
        private const string NORECURSESHORT = "nr";
        private const string NOROOTDIRECTORY = "norootdirectory";
        private const string NOROOTDIRECTORYSHORT = "nrd";
        private const string PATCHCREATEFILES = "PatchCreateFiles";
        private const string PATCHCREATEFILESSHORT = "pcf";
        private const string PATCHUPDATE = "PatchUpdate";
        private const string PATCHUPDATESHORT = "pu";
        private const string REGEXEXCLUDE = "regExExclude";
        private const string REGEXEXCLUDESHORT = "rex";
        private const string REPORTIFDIFFERENT = "ReportIfDifferent";
        private const string REPORTIFDIFFERENTSHORT = "rid";
        private const string UPDATE = "update";
        private const string UPDATESHORT = "u";
        private const string VERBOSE = "verbose";
        private const string VERBOSESHORT = "v";
        private const string WIN64VAR = "win64var";
        #endregion

        private const string DEFAULTDIRREF = "INSTALLDIR";

        // The private string to hold more detailed error information.
        private String errorMessage;

        // Indicates the error was found in OnDoneParse.
        private Boolean errorInOnDoneParse;

        /// <summary>
        /// Initializes a new instance of the ParaffinArgParser class.
        /// </summary>
        public ParaffinArgParser()
            : base(new String[] 
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
                        },
                     new String[] 
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
                            WIN64VAR,
                        },
                    true)
        {
            // Set all the appropriate defaults.
            this.FileName = String.Empty;
            this.StartDirectory = String.Empty;
            this.GroupName = String.Empty;
            this.Alias = String.Empty;
            this.DirectoryRef = String.Empty;
            this.ExtensionList = new Dictionary<String, Boolean>();
            this.IncrementValue = 1;
            this.DirectoryExcludeList = new List<String>();
            this.DiskId = 1;
            this.IncludeFiles = new List<String>();
            this.RegExExcludes = new List<Regex>();
            this.Win64 = String.Empty;

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
        /// Gets or sets the DirectoryRef Id if you want the fragment 
        /// files to go somewhere else besides the INSTALLDIR.
        /// </summary> 
        public String DirectoryRef { get; set; }

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
        /// Gets or sets the list of extensions to skip.
        /// </summary>
        public Dictionary<String, Boolean> ExtensionList { get; set; }

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
        /// Gets or sets the list of directories to exclude from the processing.
        /// </summary>
        public List<String> DirectoryExcludeList { get; set; }

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
        /// Gets or sets the list of include files to be included into the 
        /// output .WXS file. Note that there's no checking on existance or
        /// validity of these values.
        /// </summary>
        public List<String> IncludeFiles { get; set; }
        #endregion

        /// <summary>
        /// Gets or sets the list of regular expression excludes that are 
        /// applied to files and directories.
        /// </summary>
        public List<Regex> RegExExcludes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user wants to update a 
        /// previously created file.
        /// </summary>
        public Boolean Update { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the users wants verbose
        /// processing output or not.
        /// </summary>
        public Boolean Verbose { get; set; }

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
        public override void OnUsage(string errorInfo)
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
        /// One of the <see cref="SwitchStatus"/> values.
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
        protected override SwitchStatus OnSwitch(string switchSymbol,
                                                 string switchValue)
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
                        Int32 outVal = 0;
                        if (false == Int32.TryParse(switchValue, out outVal))
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
        /// One of the <see cref="SwitchStatus"/> values.
        /// </returns>
        protected override SwitchStatus OnNonSwitch(string value)
        {
            SwitchStatus ss = SwitchStatus.NoError;
            if (false == String.IsNullOrEmpty(this.FileName))
            {
                this.errorMessage = Constants.OutputAlreadySpecified;
                ss = SwitchStatus.Error;
            }
            else if (true == String.IsNullOrEmpty(value))
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
        /// One of the <see cref="SwitchStatus"/> values.
        /// </returns>
        protected override SwitchStatus OnDoneParse()
        {
            SwitchStatus ss = SwitchStatus.NoError;

            // The output file can never be null.
            if (true == string.IsNullOrEmpty(this.FileName))
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
                if (true == String.IsNullOrEmpty(this.StartDirectory))
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
                else if (true == String.IsNullOrEmpty(this.GroupName))
                {
                    this.errorMessage = Constants.GroupNameCannotBeEmpty;
                    ss = SwitchStatus.Error;
                    this.errorInOnDoneParse = true;
                }
                else if (true == this.PatchCreateFiles ||
                         true == this.PatchUpdate)
                {
                    this.errorMessage = Constants.NoPatchWhenCreating;
                    ss = SwitchStatus.Error;
                    this.errorInOnDoneParse = true;
                }

                // If no directory ref was specified, set it to the default.
                if (true == String.IsNullOrEmpty(this.DirectoryRef))
                {
                    this.DirectoryRef = DEFAULTDIRREF;
                }
            }
            else if (true == this.Update)
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
                if (true == this.PatchUpdate)
                {
                    this.errorMessage = Constants.MutuallyExclusiveOptions;
                    ss = SwitchStatus.Error;
                    this.errorInOnDoneParse = true;
                }
            }
            else if (true == this.PatchUpdate)
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
        /// One of the <see cref="SwitchStatus"/> values.
        /// </returns>
        private SwitchStatus ProcessGroupName(string switchValue)
        {
            SwitchStatus ss = SwitchStatus.NoError;
            if (false == String.IsNullOrEmpty(this.GroupName))
            {
                this.errorMessage = Constants.GroupNameMultipleSwitches;
                ss = SwitchStatus.Error;
            }
            else if (true == String.IsNullOrEmpty(switchValue))
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
