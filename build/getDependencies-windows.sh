#!/bin/bash
# server=build.palaso.org
# project=Bloom
# build=Bloom-5.1-BetaInternal-Continuous
# root_dir=..
# Auto-generated by https://github.com/chrisvire/BuildUpdate.
# Do not edit this file by hand!

cd "$(dirname "$0")"

# *** Functions ***
force=0
clean=0

while getopts fc opt; do
case $opt in
f) force=1 ;;
c) clean=1 ;;
esac
done

shift $((OPTIND - 1))

copy_auto() {
if [ "$clean" == "1" ]
then
echo cleaning $2
rm -f ""$2""
else
where_curl=$(type -P curl)
where_wget=$(type -P wget)
if [ "$where_curl" != "" ]
then
copy_curl "$1" "$2"
elif [ "$where_wget" != "" ]
then
copy_wget "$1" "$2"
else
echo "Missing curl or wget"
exit 1
fi
fi
}

copy_curl() {
echo "curl: $2 <= $1"
if [ -e "$2" ] && [ "$force" != "1" ]
then
curl -# -L -z "$2" -o "$2" "$1"
else
curl -# -L -o "$2" "$1"
fi
}

copy_wget() {
echo "wget: $2 <= $1"
f1=$(basename $1)
f2=$(basename $2)
cd $(dirname $2)
wget -q -L -N "$1"
# wget has no true equivalent of curl's -o option.
# Different versions of wget handle (or not) % escaping differently.
# A URL query is the only reason why $f1 and $f2 should differ.
if [ "$f1" != "$f2" ]; then mv $f2\?* $f2; fi
cd -
}


# *** Results ***
# build: Bloom-5.1-BetaInternal-Continuous (Bloom_Bloom51BetaInternalContinuous)
# project: Bloom
# URL: https://build.palaso.org/viewType.html?buildTypeId=Bloom_Bloom51BetaInternalContinuous
# VCS: git://github.com/BloomBooks/BloomDesktop.git [refs/heads/Version5.1]
# dependencies:
# [0] build: bloom-win32-static-dependencies (bt396)
#     project: Bloom
#     URL: https://build.palaso.org/viewType.html?buildTypeId=bt396
#     clean: false
#     revision: bloom-5.1.tcbuildtag
#     paths: {"ghostscript-win32.zip!**"=>"DistFiles/ghostscript", "optipng-0.7.4-win32/optipng.exe"=>"DistFiles", "connections.dll"=>"DistFiles", "MSBuild.Community.Tasks.dll"=>"build", "MSBuild.Community.Tasks.Targets"=>"build", "Lame.zip!**"=>"lib/lame", "gm.zip!**"=>"lib", "FirefoxDependencies.zip!**"=>"lib/FirefoxDependencies"}
# [1] build: PortableDevices (from PodcastUtilities) (Bloom_PortableDevicesFromPodcastUtitlies)
#     project: Bloom
#     URL: https://build.palaso.org/viewType.html?buildTypeId=Bloom_PortableDevicesFromPodcastUtitlies
#     clean: false
#     revision: bloom-5.1.tcbuildtag
#     paths: {"PodcastUtilities.PortableDevices.dll"=>"lib/dotnet", "PodcastUtilities.PortableDevices.pdb"=>"lib/dotnet", "Interop.PortableDeviceApiLib.dll"=>"lib/dotnet", "Interop.PortableDeviceTypesLib.dll"=>"lib/dotnet"}
#     VCS: https://github.com/BloomBooks/PodcastUtilities.git [refs/heads/master]
# [2] build: Squirrel (Bloom_Squirrel)
#     project: Bloom
#     URL: https://build.palaso.org/viewType.html?buildTypeId=Bloom_Squirrel
#     clean: false
#     revision: bloom-5.1.tcbuildtag
#     paths: {"*.*"=>"lib/dotnet"}
#     VCS: https://github.com/BloomBooks/Squirrel.Windows.git [refs/heads/master]
# [3] build: Bloom Help 5.1 (Bloom_Help_BloomHelp51)
#     project: Help
#     URL: https://build.palaso.org/viewType.html?buildTypeId=Bloom_Help_BloomHelp51
#     clean: false
#     revision: latest.lastSuccessful
#     paths: {"*.chm"=>"DistFiles"}
# [4] build: pdf.js (bt401)
#     project: BuildTasks
#     URL: https://build.palaso.org/viewType.html?buildTypeId=bt401
#     clean: false
#     revision: bloom-5.1.tcbuildtag
#     paths: {"pdfjs-viewer.zip!**"=>"DistFiles/pdf"}
#     VCS: https://github.com/mozilla/pdf.js.git [gh-pages]
# [5] build: GeckofxHtmlToPdf-Win32-continuous (bt463)
#     project: GeckofxHtmlToPdf
#     URL: https://build.palaso.org/viewType.html?buildTypeId=bt463
#     clean: false
#     revision: bloom-5.1.tcbuildtag
#     paths: {"Args.dll"=>"lib/dotnet", "GeckofxHtmlToPdf.exe"=>"lib/dotnet", "GeckofxHtmlToPdf.exe.config"=>"lib/dotnet"}
#     VCS: https://github.com/BloomBooks/geckofxHtmlToPdf [refs/heads/master]
# [6] build: PdfDroplet-Win-master-Continuous (bt54)
#     project: PdfDroplet
#     URL: https://build.palaso.org/viewType.html?buildTypeId=bt54
#     clean: false
#     revision: bloom-5.1.tcbuildtag
#     paths: {"PdfDroplet.exe"=>"lib/dotnet", "PdfSharp.dll"=>"lib/dotnet"}
#     VCS: https://github.com/sillsdev/pdfDroplet [master]
# [7] build: TidyManaged-master-win32-continuous (bt349)
#     project: TidyManaged
#     URL: https://build.palaso.org/viewType.html?buildTypeId=bt349
#     clean: false
#     revision: bloom-5.1.tcbuildtag
#     paths: {"*.*"=>"lib/dotnet"}
#     VCS: https://github.com/BloomBooks/TidyManaged.git [master]
# [8] build: Windows master continuous (XliffForHtml_WindowsMasterContinuous)
#     project: XliffForHtml
#     URL: https://build.palaso.org/viewType.html?buildTypeId=XliffForHtml_WindowsMasterContinuous
#     clean: false
#     revision: bloom-5.1.tcbuildtag
#     paths: {"HtmlXliff.*"=>"lib/dotnet", "HtmlAgilityPack.*"=>"lib/dotnet"}
#     VCS: https://github.com/sillsdev/XliffForHtml [refs/heads/master]

# make sure output directories exist
mkdir -p ../DistFiles
mkdir -p ../DistFiles/ghostscript
mkdir -p ../DistFiles/pdf
mkdir -p ../Downloads
mkdir -p ../build
mkdir -p ../lib
mkdir -p ../lib/FirefoxDependencies
mkdir -p ../lib/dotnet
mkdir -p ../lib/lame

# download artifact dependencies
copy_auto https://build.palaso.org/guestAuth/repository/download/bt396/bloom-5.1.tcbuildtag/ghostscript-win32.zip ../Downloads/ghostscript-win32.zip
copy_auto https://build.palaso.org/guestAuth/repository/download/bt396/bloom-5.1.tcbuildtag/optipng-0.7.4-win32/optipng.exe ../DistFiles/optipng.exe
copy_auto https://build.palaso.org/guestAuth/repository/download/bt396/bloom-5.1.tcbuildtag/connections.dll ../DistFiles/connections.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/bt396/bloom-5.1.tcbuildtag/MSBuild.Community.Tasks.dll ../build/MSBuild.Community.Tasks.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/bt396/bloom-5.1.tcbuildtag/MSBuild.Community.Tasks.Targets ../build/MSBuild.Community.Tasks.Targets
copy_auto https://build.palaso.org/guestAuth/repository/download/bt396/bloom-5.1.tcbuildtag/Lame.zip ../Downloads/Lame.zip
copy_auto https://build.palaso.org/guestAuth/repository/download/bt396/bloom-5.1.tcbuildtag/gm.zip ../Downloads/gm.zip
copy_auto https://build.palaso.org/guestAuth/repository/download/bt396/bloom-5.1.tcbuildtag/FirefoxDependencies.zip ../Downloads/FirefoxDependencies.zip
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_PortableDevicesFromPodcastUtitlies/bloom-5.1.tcbuildtag/PodcastUtilities.PortableDevices.dll ../lib/dotnet/PodcastUtilities.PortableDevices.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_PortableDevicesFromPodcastUtitlies/bloom-5.1.tcbuildtag/PodcastUtilities.PortableDevices.pdb ../lib/dotnet/PodcastUtilities.PortableDevices.pdb
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_PortableDevicesFromPodcastUtitlies/bloom-5.1.tcbuildtag/Interop.PortableDeviceApiLib.dll ../lib/dotnet/Interop.PortableDeviceApiLib.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_PortableDevicesFromPodcastUtitlies/bloom-5.1.tcbuildtag/Interop.PortableDeviceTypesLib.dll ../lib/dotnet/Interop.PortableDeviceTypesLib.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/7z.dll ../lib/dotnet/7z.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/7z.exe ../lib/dotnet/7z.exe
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/DeltaCompressionDotNet.MsDelta.dll ../lib/dotnet/DeltaCompressionDotNet.MsDelta.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/DeltaCompressionDotNet.PatchApi.dll ../lib/dotnet/DeltaCompressionDotNet.PatchApi.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/DeltaCompressionDotNet.dll ../lib/dotnet/DeltaCompressionDotNet.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/Microsoft.Data.Edm.dll ../lib/dotnet/Microsoft.Data.Edm.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/Microsoft.Data.Edm.xml ../lib/dotnet/Microsoft.Data.Edm.xml
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/Microsoft.Data.OData.dll ../lib/dotnet/Microsoft.Data.OData.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/Microsoft.Data.OData.xml ../lib/dotnet/Microsoft.Data.OData.xml
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/Microsoft.Data.Services.Client.dll ../lib/dotnet/Microsoft.Data.Services.Client.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/Microsoft.Data.Services.Client.xml ../lib/dotnet/Microsoft.Data.Services.Client.xml
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/Microsoft.Web.XmlTransform.dll ../lib/dotnet/Microsoft.Web.XmlTransform.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/Mono.Cecil.dll ../lib/dotnet/Mono.Cecil.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/NuGet.Squirrel.dll ../lib/dotnet/NuGet.Squirrel.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/Setup.exe ../lib/dotnet/Setup.exe
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/SharpCompress.dll ../lib/dotnet/SharpCompress.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/Splat.dll ../lib/dotnet/Splat.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/Squirrel.dll ../lib/dotnet/Squirrel.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/StubExecutable.exe ../lib/dotnet/StubExecutable.exe
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/SyncReleases.exe ../lib/dotnet/SyncReleases.exe
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/System.Spatial.dll ../lib/dotnet/System.Spatial.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/System.Spatial.xml ../lib/dotnet/System.Spatial.xml
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/Update-Mono.exe ../lib/dotnet/Update-Mono.exe
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/Update.com ../lib/dotnet/Update.com
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/Update.exe ../lib/dotnet/Update.exe
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/WriteZipToSetup.exe ../lib/dotnet/WriteZipToSetup.exe
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/rcedit.exe ../lib/dotnet/rcedit.exe
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Squirrel/bloom-5.1.tcbuildtag/signtool.exe ../lib/dotnet/signtool.exe
copy_auto https://build.palaso.org/guestAuth/repository/download/Bloom_Help_BloomHelp51/latest.lastSuccessful/Bloom.chm ../DistFiles/Bloom.chm
copy_auto https://build.palaso.org/guestAuth/repository/download/bt401/bloom-5.1.tcbuildtag/pdfjs-viewer.zip ../Downloads/pdfjs-viewer.zip
copy_auto https://build.palaso.org/guestAuth/repository/download/bt463/bloom-5.1.tcbuildtag/Args.dll ../lib/dotnet/Args.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/bt463/bloom-5.1.tcbuildtag/GeckofxHtmlToPdf.exe ../lib/dotnet/GeckofxHtmlToPdf.exe
copy_auto https://build.palaso.org/guestAuth/repository/download/bt463/bloom-5.1.tcbuildtag/GeckofxHtmlToPdf.exe.config ../lib/dotnet/GeckofxHtmlToPdf.exe.config
copy_auto https://build.palaso.org/guestAuth/repository/download/bt54/bloom-5.1.tcbuildtag/PdfDroplet.exe ../lib/dotnet/PdfDroplet.exe
copy_auto https://build.palaso.org/guestAuth/repository/download/bt54/bloom-5.1.tcbuildtag/PdfSharp.dll ../lib/dotnet/PdfSharp.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/bt349/bloom-5.1.tcbuildtag/TidyManaged.dll ../lib/dotnet/TidyManaged.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/bt349/bloom-5.1.tcbuildtag/TidyManaged.dll.config ../lib/dotnet/TidyManaged.dll.config
copy_auto https://build.palaso.org/guestAuth/repository/download/bt349/bloom-5.1.tcbuildtag/libtidy.dll ../lib/dotnet/libtidy.dll
copy_auto https://build.palaso.org/guestAuth/repository/download/XliffForHtml_WindowsMasterContinuous/bloom-5.1.tcbuildtag/HtmlXliff.exe ../lib/dotnet/HtmlXliff.exe
copy_auto https://build.palaso.org/guestAuth/repository/download/XliffForHtml_WindowsMasterContinuous/bloom-5.1.tcbuildtag/HtmlXliff.pdb ../lib/dotnet/HtmlXliff.pdb
copy_auto https://build.palaso.org/guestAuth/repository/download/XliffForHtml_WindowsMasterContinuous/bloom-5.1.tcbuildtag/HtmlAgilityPack.dll ../lib/dotnet/HtmlAgilityPack.dll
# extract downloaded zip files
unzip -uqo ../Downloads/ghostscript-win32.zip -d "../DistFiles/ghostscript"
unzip -uqo ../Downloads/Lame.zip -d "../lib/lame"
unzip -uqo ../Downloads/gm.zip -d "../lib"
unzip -uqo ../Downloads/FirefoxDependencies.zip -d "../lib/FirefoxDependencies"
unzip -uqo ../Downloads/pdfjs-viewer.zip -d "../DistFiles/pdf"
# End of script
