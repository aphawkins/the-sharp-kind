# 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using namespace System.Collections.Generic
using namespace SharpKind.Security.Cryptography
using namespace SharpKind.Security.Cryptography.Cryptanalysis

# Each cipher's class, its settings class (none for the keyless ciphers), and, where a hashtable
# cannot simply set the settings' properties, a script block that builds the settings from one.
$script:Ciphers = [ordered]@{
    Adfgvx         = @{ Type = [Adfgvx]; SettingsType = [AdfgvxSettings]; Convert = { ConvertTo-AdfgvxSettings $args[0] } }
    Affine         = @{ Type = [Affine]; SettingsType = [AffineSettings] }
    Atbash         = @{ Type = [Atbash] }
    Autokey        = @{ Type = [Autokey]; SettingsType = [VigenereSettings] }
    Beaufort       = @{ Type = [Beaufort]; SettingsType = [VigenereSettings] }
    Book           = @{ Type = [BookCipher]; SettingsType = [BookCipherSettings]; Convert = { ConvertTo-BookCipherSettings $args[0] } }
    Caesar         = @{ Type = [Caesar]; SettingsType = [CaesarSettings] }
    Columnar       = @{ Type = [Columnar]; SettingsType = [ColumnarSettings] }
    Enigma         = @{ Type = [Enigma]; SettingsType = [EnigmaSettings]; Convert = { ConvertTo-EnigmaSettings $args[0] } }
    Homophonic     = @{ Type = [Homophonic]; SettingsType = [HomophonicSettings]; Convert = { ConvertTo-HomophonicSettings $args[0] } }
    MonoAlphabetic = @{ Type = [MonoAlphabetic]; SettingsType = [MonoAlphabeticSettings] }
    Playfair       = @{ Type = [Playfair]; SettingsType = [PlayfairSettings] }
    RailFence      = @{ Type = [RailFence]; SettingsType = [RailFenceSettings] }
    Reflector      = @{ Type = [Reflector]; SettingsType = [ReflectorSettings]; Convert = { ConvertTo-ReflectorSettings $args[0] } }
    Rot13          = @{ Type = [Rot13] }
    Vigenere       = @{ Type = [Vigenere]; SettingsType = [VigenereSettings] }
}

class CipherNames : System.Management.Automation.IValidateSetValuesGenerator {
    [string[]] GetValidValues() { return [string[]]$script:Ciphers.Keys }
}

function Assert-SettingsKey {
    param([System.Collections.IDictionary] $Settings, [string[]] $Allowed)

    foreach ($key in $Settings.Keys) {
        if ($key -notin $Allowed) {
            throw "Unknown setting '$key'. Valid settings are: $($Allowed -join ', ')."
        }
    }
}

function ConvertTo-AdfgvxSettings {
    param([System.Collections.IDictionary] $Settings)

    Assert-SettingsKey $Settings 'Labels', 'Square', 'Keyword'
    $default = [AdfgvxSettings]::new()
    [AdfgvxSettings]::new(
        ($Settings.Labels ?? $default.Labels),
        ($Settings.Square ?? $default.Square),
        ($Settings.Keyword ?? $default.Keyword))
}

function ConvertTo-BookCipherSettings {
    param([System.Collections.IDictionary] $Settings)

    Assert-SettingsKey $Settings 'KeyText'
    [BookCipherSettings]::new($Settings.KeyText)
}

function ConvertTo-HomophonicSettings {
    param([System.Collections.IDictionary] $Settings)

    $homophones = [Dictionary[char, IReadOnlyList[string]]]::new()

    foreach ($key in $Settings.Keys) {
        if ("$key".Length -ne 1) {
            throw "Homophonic setting '$key' must be a single plaintext letter."
        }

        $homophones[[char]"$key"] = [string[]]@($Settings[$key])
    }

    [HomophonicSettings]::new($homophones)
}

function ConvertTo-ReflectorSettings {
    param([System.Collections.IDictionary] $Settings)

    Assert-SettingsKey $Settings 'CharacterSet', 'Substitutions'
    $properties = [ordered]@{}

    # The character set must be set before the substitutions.
    foreach ($name in 'CharacterSet', 'Substitutions') {
        if ($Settings.Contains($name)) {
            $properties[$name] = [List[char]][char[]]"$($Settings[$name])"
        }
    }

    New-Object ReflectorSettings -Property $properties
}

function ConvertTo-EnigmaSettings {
    param([System.Collections.IDictionary] $Settings)

    Assert-SettingsKey $Settings 'Reflector', 'Rotors', 'Rings', 'Positions', 'Plugboard'

    # Rotors, rings and positions read left to right, as on the machine, so the last is the fastest.
    $positions = [EnigmaRotorPosition]::Third, [EnigmaRotorPosition]::Second, [EnigmaRotorPosition]::Fastest
    $rotorNumbers = @($Settings.Rotors ?? ('I', 'II', 'III'))
    $rings = @($Settings.Rings ?? (1, 1, 1))
    $letters = [char[]]"$($Settings.Positions ?? 'AAA')"

    foreach ($list in @{ Rotors = $rotorNumbers; Rings = $rings; Positions = $letters }.GetEnumerator()) {
        if ($list.Value.Count -ne 3) {
            throw "Enigma setting '$($list.Key)' must have 3 values, left to right."
        }
    }

    $rotors = [Dictionary[EnigmaRotorPosition, IEnigmaRotor]]::new()

    for ($i = 0; $i -lt 3; $i++) {
        $rotor = [EnigmaRotor]::new()

        # Setting the rotor number resets the ring and position, so it goes first.
        $rotor.RotorNumber = [EnigmaRotorNumber]"$($rotorNumbers[$i])"
        $rotor.RingPosition = $rings[$i]
        $rotor.CurrentSetting = [char]::ToUpperInvariant($letters[$i])
        $rotors[$positions[$i]] = $rotor
    }

    $pairs = [List[EnigmaPlugboardPair]]::new()

    foreach ($pair in "$($Settings.Plugboard)".Split(' ', [StringSplitOptions]::RemoveEmptyEntries)) {
        if ($pair.Length -ne 2) {
            throw "Enigma plugboard pair '$pair' must be 2 letters."
        }

        $pairs.Add([EnigmaPlugboardPair]@{ From = [char]::ToUpperInvariant($pair[0]); To = [char]::ToUpperInvariant($pair[1]) })
    }

    $reflector = [EnigmaReflector]::new()
    $reflector.ReflectorNumber = [EnigmaReflectorNumber]"$($Settings.Reflector ?? 'B')"

    New-Object EnigmaSettings -Property @{
        Reflector = $reflector
        Rotors    = [EnigmaRotors]::new($rotors)
        Plugboard = [EnigmaPlugboard]::new($pairs)
    }
}

function ConvertTo-CipherSettings {
    param([string] $Cipher, [object] $Settings)

    $definition = $script:Ciphers[$Cipher]

    if ($null -eq $definition.SettingsType) {
        if ($null -ne $Settings) {
            throw "Cipher '$Cipher' has no settings."
        }

        return $null
    }

    if ($null -eq $Settings) {
        throw "Cipher '$Cipher' needs -Settings."
    }

    if ($Settings -isnot [System.Collections.IDictionary]) {
        # Already a settings object, from New-CipherSettings or Find-CipherKey.
        return $Settings
    }

    if ($definition.Convert) {
        return & $definition.Convert $Settings
    }

    # The character set must be set before the substitutions.
    $properties = [ordered]@{}
    $Settings.Keys | Sort-Object { $_ -ne 'CharacterSet' } | ForEach-Object { $properties[$_] = $Settings[$_] }
    New-Object $definition.SettingsType -Property $properties
}

function New-CipherInstance {
    param([string] $Cipher, [object] $Settings)

    $definition = $script:Ciphers[$Cipher]
    $converted = ConvertTo-CipherSettings $Cipher $Settings

    if ($null -eq $definition.SettingsType) {
        return $definition.Type::new()
    }

    $definition.Type::new($converted)
}

function Invoke-Cipher {
    param([ICipher] $Instance, [string] $Text, [switch] $Decrypt)

    $result = $Decrypt ? $Instance.Decrypt($Text) : $Instance.Encrypt($Text)

    # Enigma advances its rotors as it runs. Winding them back makes each piece of text a separate
    # message with the same key, and leaves the caller's settings object as it was.
    if ($Instance -is [Enigma]) {
        $Instance.Reset()
    }

    $result
}

<#
.SYNOPSIS
Gets settings for a cipher.

.DESCRIPTION
Returns the cipher's default settings, or random settings with -Random. Pass the result to the
-Settings parameter of Protect-CipherText or Unprotect-CipherText.

.EXAMPLE
$settings = New-CipherSettings -Cipher Enigma -Random
#>
function New-CipherSettings {
    [CmdletBinding()]
    [OutputType([object])]
    param(
        [Parameter(Mandatory, Position = 0)]
        [ValidateSet([CipherNames])]
        [string] $Cipher,

        [switch] $Random
    )

    $definition = $script:Ciphers[$Cipher]

    if ($null -eq $definition.SettingsType) {
        throw "Cipher '$Cipher' has no settings."
    }

    if ($definition.Type -eq [BookCipher]) {
        throw "Cipher 'Book' has no default or random settings. Pass -Settings @{ KeyText = '...' } instead."
    }

    $instance = $definition.Type::new($definition.SettingsType::new())

    if ($Random) {
        $instance.GenerateSettings()
    }

    $instance.Settings
}

<#
.SYNOPSIS
Encrypts text with a classical cipher.

.DESCRIPTION
Settings are a settings object from New-CipherSettings or Find-CipherKey, or a hashtable of the
settings' properties. Each piece of pipeline input is encrypted as a separate message.

.EXAMPLE
'Attack at dawn' | Protect-CipherText -Cipher Caesar -Settings @{ RightShift = 3 }

.EXAMPLE
Protect-CipherText -Cipher Enigma -Settings @{ Rotors = 'I', 'II', 'III'; Positions = 'AAA' } -Text 'AAAAA'
#>
function Protect-CipherText {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory, Position = 0)]
        [ValidateSet([CipherNames])]
        [string] $Cipher,

        [Parameter(Position = 1)]
        [object] $Settings,

        [Parameter(Mandatory, ValueFromPipeline)]
        [AllowEmptyString()]
        [string] $Text
    )

    begin { $instance = New-CipherInstance $Cipher $Settings }
    process { Invoke-Cipher $instance $Text }
}

<#
.SYNOPSIS
Decrypts text with a classical cipher.

.DESCRIPTION
Takes the same settings as Protect-CipherText. Each piece of pipeline input is decrypted as a
separate message.

.EXAMPLE
'DWWDFN DW GDZQ' | Unprotect-CipherText -Cipher Caesar -Settings @{ RightShift = 3 }
#>
function Unprotect-CipherText {
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter(Mandatory, Position = 0)]
        [ValidateSet([CipherNames])]
        [string] $Cipher,

        [Parameter(Position = 1)]
        [object] $Settings,

        [Parameter(Mandatory, ValueFromPipeline)]
        [AllowEmptyString()]
        [string] $Text
    )

    begin { $instance = New-CipherInstance $Cipher $Settings }
    process { Invoke-Cipher $instance $Text -Decrypt }
}

<#
.SYNOPSIS
Cracks ciphertext without the key.

.DESCRIPTION
Finds the most likely settings by statistical analysis of the plaintext language, and returns
them with the plaintext they give. Caesar and MonoAlphabetic assume English. Homophonic takes
-Language. Cracking needs a reasonable length of ciphertext: a few hundred letters for the
substitution ciphers.

.EXAMPLE
'WKH TXLFN EURZQ IRA' | Find-CipherKey -Cipher Caesar
#>
function Find-CipherKey {
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param(
        [Parameter(Mandatory, Position = 0)]
        [ValidateSet('Caesar', 'MonoAlphabetic', 'Homophonic')]
        [string] $Cipher,

        [Parameter(Mandatory, ValueFromPipeline)]
        [string] $Text,

        [Language] $Language = [Language]::English
    )

    process {
        if ($Cipher -ne 'Homophonic' -and $Language -ne [Language]::English) {
            throw "Cipher '$Cipher' can only be cracked in English."
        }

        $settings = switch ($Cipher) {
            'Caesar' { [CaesarSettings]@{ RightShift = [CaesarCryptanalysis]::Crack($Text).Item1 } }
            'MonoAlphabetic' { [MonoAlphabeticCryptanalysis]::Crack($Text) }
            'Homophonic' { [HomophonicCryptanalysis]::Crack($Text, $Language) }
        }

        [pscustomobject]@{
            Cipher    = $Cipher
            Settings  = $settings
            Plaintext = Invoke-Cipher (New-CipherInstance $Cipher $settings) $Text -Decrypt
        }
    }
}

Export-ModuleMember -Function New-CipherSettings, Protect-CipherText, Unprotect-CipherText, Find-CipherKey
