# Building the MSI
The Installer project does not have a reference to the CA Service project because MSBuild does not support resolving COM references, which are contained in the CAService project. 
Therefore, to build this installer, the CA Service has to be built manually beforehand.