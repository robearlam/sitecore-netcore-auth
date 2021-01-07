# sitecore-netcore-auth
A repository showing how to enable authentication in a .NET Core headless Sitecore application.

# Setup
.\init.ps1 -InitEnv -LicenseXmlPath "<<LICENSE_PATH>>" -AdminPassword "b"
.\up.ps1

# Create a test user
Create a Sitecore user with the username `DotNetUser` and a password of your choice

# Test Login
Access `https://www.sitecore_netcore_auth.localhost/Login` and attempt to login with the users details you created.