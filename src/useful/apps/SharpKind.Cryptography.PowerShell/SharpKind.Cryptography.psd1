# 'SharpKind Libraries' - Andy Hawkins 2023-2026.

@{
    RootModule           = 'SharpKind.Cryptography.psm1'

    # build.ps1 stamps the version from MinVer.
    ModuleVersion        = '0.0.0'
    GUID                 = 'b7e3c2a4-5f1d-4e8a-9c6b-2d0f8a1e7b35'
    Author               = 'Andy Hawkins'
    Copyright            = '(c) Andy Hawkins 2023-2026. All rights reserved.'
    Description          = 'Encrypt, decrypt and crack classical ciphers: ADFGVX, Affine, Atbash, Autokey, Beaufort, Book, Caesar, Columnar, Enigma, Homophonic, MonoAlphabetic, Playfair, Rail Fence, Reflector, ROT13 and Vigenere.'

    # The SharpKind libraries target .NET 10, which PowerShell 7.6 is the first to run on.
    PowerShellVersion    = '7.6'
    CompatiblePSEditions = @('Core')

    RequiredAssemblies   = @(
        'lib/SharpKind.Security.Cryptography.dll'
        'lib/SharpKind.Security.Cryptography.Cryptanalysis.dll'
    )

    FunctionsToExport    = @('New-CipherSettings', 'Protect-CipherText', 'Unprotect-CipherText', 'Find-CipherKey')
    CmdletsToExport      = @()
    VariablesToExport    = @()
    AliasesToExport      = @()

    PrivateData          = @{
        PSData = @{
            Tags       = @('cipher', 'cryptography', 'cryptanalysis', 'enigma', 'caesar', 'vigenere', 'PSEdition_Core')
            LicenseUri = 'https://github.com/aphawkins/the-sharp-kind/blob/main/LICENSE'
            ProjectUri = 'https://github.com/aphawkins/the-sharp-kind'
            Prerelease = ''
        }
    }
}
