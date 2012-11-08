# Paraffin - Making WiX Easier #

[Windows Installer XML ](http://wixtoolset.org "WiX") is a fanstastic tool for building installers. One small weakness with WiX is keeping your file fragments up to date. While the Heat tool creates the fragments easily, you have to manually edit the .WXS when adding and removing files. Paraffin has been around for a while, but my original requirements sum up the project nicely.

My goal for PARAFFIN was that it would build immediately consumable WiX fragments with a minimum of fuss on your part. I wanted PARAFFIN to meet the following requirements for initially creating a .WXS fragment for a directory:

- PARRAFIN.EXE created unique values to the Component, Directory, and File elements Id attribute so you do not have to worry about conflicts across large projects
- PARAFIN.EXE creates a ComponentGroup element in the output file with all Component elements in the file automatically specified with ComponentRef values
- You can optionally exclude specific file extensions from being added to the .WXS fragment
- You can optionally exclude directories from inclusion by specifying a partial name
- You can optionally specify if you want GUID values automatically generated for all components
- You can optionally specify multiple files per Component (the default is one file per component)
- You can optionally specify that you do not want to recurse directories other than the one specified
- You can optionally specify an alias for the directory name when setting the File element Source attribute so you do not have hard coded drive and directory names in the output .WXS file

After you've created a .WXS fragment with PARAFFIN, you don't want to have to edit the fragment manually, so I wanted PARAFFIN to meet the following requirements for creating an updated output file from an existing .WXS fragment:

- The updated output is written to a .PARAFFIN file so the original .WXS fragment is not disturbed
- All command line options specified when creating the initial .WXS fragment are automatically set when updating a file created by PARAFFIN
- Any new directories and files found are automatically added to the output file
- Any directories and files that are no longer part of the directory structure are removed from the output file

Since the original release of Paraffin a few other requirements have been added:

- You can specify the directory reference when initially creating the WXS file
- Directories and files can be excluded by name or regular expression
- Support for adding fragment information through .ParaffinMold files so you can more easily support installation options such as services
- Adding include files to the top of the produced .WXS file
- Specifying the DiskID is supported so you can handle giant installers easier
- Full upport for minor upgrades with transitive properties set so you can remove files on the minor upgrade and produce the zero byte files so your installer builds
- Support for reporting if subsequent runs of Paraffin are reporting different output (i.e., adding or removing files)

## Getting Started with Paraffin ##
Obviously, a prior working knowledge of WiX is required so head over to the [WiX Tutorial](http://wix.tramontana.co.hu/) for more information. After downloading the project, look at the Zen of Paraffin document in the root directory.

## Want to Contribute? ##
Have at it! Fork away and have fun, but do send me the patches so I can put your cool features into the master branch. All you need is any version of Visual Studio 2010 or Visual Studio 2012.

## Have a Question? ##
Feel free to email me (john AT wintellect.com) or on the project discussions.

## Special Thanks! ##
I greatly appreciate everyone using Paraffin and especially those of you who have contributed code or bug reports. Who knew a little project I did to learn LINQ to XML could be so useful to so many?
