Write-Host $Env:TRAVIS_BUILD_DIR
Write-Host $Env:TRAVIS_BUILD_NUMBER

$currentVersion = $Env:TRAVIS_BUILD_NUMBER
Write-Host	$currentVersion

$nl = [Environment]::NewLine

# Apply the version to the assembly property files
$files = gci $Env:TRAVIS_BUILD_DIR -recurse -include "*Properties*","My Project" | 
	?{ $_.PSIsContainer } | 
	foreach { gci -Path $_.FullName -Recurse -include AssemblyInfo.* }
if($files)
{
	foreach ($file in $files) {
			
			$filecontent = Get-Content($file)
			
			if($filecontent -match "AssemblyVersion"){
				Write-Host "Found AssemblyVersion"
                $filecontent = $filecontent -replace "AssemblyVersion\(.*\)", "AssemblyVersion(""$currentVersion"")" 
				Write-Host "Updated AssemblyVersion"
            }
            else{
				Write-Host "New AssemblyVersion"
                $filecontent = $filecontent + $nl + "[assembly: AssemblyVersion(""$currentVersion"")]"
				Write-Host "Updated AssemblyVersion"
            }
			
			if($filecontent -match "AssemblyFileVersion"){
				Write-Host "Found AssemblyFileVersion"
                $filecontent = $filecontent -replace "AssemblyFileVersion\(.*\)", "AssemblyFileVersion(""$currentVersion"")" 
				Write-Host "Updated AssemblyFileVersion"
            }
            else{
				Write-Host "New AssemblyFileVersion"
                $filecontent = $filecontent + $nl + "[assembly: AssemblyFileVersion(""$currentVersion"")]"
				Write-Host "Updated AssemblyFileVersion"


            }
			
			# Output the updated content to the original file (first make sure it's writeable)
			attrib $file -r
            $filecontent | Out-File $file
	}
}
else
{
	Write-Warning "Found no files."
}