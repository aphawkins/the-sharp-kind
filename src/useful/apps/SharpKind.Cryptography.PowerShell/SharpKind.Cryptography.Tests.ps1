# 'SharpKind Libraries' - Andy Hawkins 2023-2026.

# Runs against the built module: run build.ps1 first, or set SHARPKIND_CRYPTOGRAPHY_MODULE to its folder.

BeforeAll {
    $modulePath = $env:SHARPKIND_CRYPTOGRAPHY_MODULE ?? (Join-Path $PSScriptRoot '../../../../publish/SharpKind.Cryptography')
    Import-Module $modulePath -Force
}

Describe 'Module' {
    It 'exports the four commands' {
        (Get-Command -Module SharpKind.Cryptography).Name | Sort-Object |
            Should -Be @('Find-CipherKey', 'New-CipherSettings', 'Protect-CipherText', 'Unprotect-CipherText')
    }
}

Describe 'Protect-CipherText' {
    It 'encrypts <Cipher> with known settings' -ForEach @(
        @{ Cipher = 'Atbash'; Settings = $null; Plaintext = 'ABC xyz'; Ciphertext = 'ZYX CBA' }
        @{ Cipher = 'Rot13'; Settings = $null; Plaintext = 'HELLO'; Ciphertext = 'URYYB' }
        @{ Cipher = 'Caesar'; Settings = @{ RightShift = 3 }; Plaintext = 'Attack at dawn'; Ciphertext = 'DWWDFN DW GDZQ' }
        @{ Cipher = 'Vigenere'; Settings = @{ Keyword = 'LEMON' }; Plaintext = 'ATTACKATDAWN'; Ciphertext = 'LXFOPVEFRNHR' }
        @{ Cipher = 'Enigma'; Settings = @{ Reflector = 'B'; Rotors = 'I', 'II', 'III'; Rings = 1, 1, 1; Positions = 'AAA' }; Plaintext = 'AAAAA'; Ciphertext = 'BDZGO' }
        @{ Cipher = 'Reflector'; Settings = @{ Substitutions = 'BADCFEHGJILKNMPORQTSVUXWZY' }; Plaintext = 'ABCD'; Ciphertext = 'BADC' }
    ) {
        Protect-CipherText -Cipher $Cipher -Settings $Settings -Text $Plaintext | Should -BeExactly $Ciphertext
    }

    It 'encrypts each piece of pipeline input as a separate Enigma message' {
        $settings = @{ Rotors = 'I', 'II', 'III' }

        'AAAAA', 'AAAAA' | Protect-CipherText -Cipher Enigma -Settings $settings | Should -BeExactly 'BDZGO', 'BDZGO'
    }

    It 'leaves an Enigma settings object as it was' {
        $settings = New-CipherSettings -Cipher Enigma -Random
        $ciphertext = Protect-CipherText -Cipher Enigma -Settings $settings -Text 'HELLO WORLD'

        Unprotect-CipherText -Cipher Enigma -Settings $settings -Text $ciphertext | Should -BeExactly 'HELLO WORLD'
    }

    It 'needs settings for <_>' -ForEach 'Caesar', 'Enigma', 'Book' {
        { Protect-CipherText -Cipher $_ -Text 'ABC' } | Should -Throw "*needs -Settings*"
    }

    It 'rejects settings for a cipher without them' {
        { Protect-CipherText -Cipher Atbash -Settings @{ RightShift = 3 } -Text 'ABC' } | Should -Throw '*has no settings*'
    }

    It 'rejects an unknown <Cipher> setting' -ForEach @(
        @{ Cipher = 'Caesar' }
        @{ Cipher = 'Enigma' }
        @{ Cipher = 'Adfgvx' }
    ) {
        { Protect-CipherText -Cipher $Cipher -Settings @{ Nonsense = 1 } -Text 'ABC' } | Should -Throw
    }

    It 'rejects an unknown cipher' {
        { Protect-CipherText -Cipher Nonsense -Settings $null -Text 'ABC' } | Should -Throw
    }
}

Describe 'Round trip' {
    It 'decrypts what <Cipher> encrypts' -ForEach @(
        @{ Cipher = 'Adfgvx'; Settings = @{ Keyword = 'PRIVACY' } }
        @{ Cipher = 'Affine'; Settings = @{ Multiplier = 5; Shift = 8 } }
        @{ Cipher = 'Atbash'; Settings = $null }
        @{ Cipher = 'Autokey'; Settings = @{ Keyword = 'QUEEN' } }
        @{ Cipher = 'Beaufort'; Settings = @{ Keyword = 'FORTIFICATION' } }
        @{ Cipher = 'Book'; Settings = @{ KeyText = 'The quick brown fox jumps over the lazy dog.' } }
        @{ Cipher = 'Caesar'; Settings = @{ RightShift = 7 } }
        @{ Cipher = 'Columnar'; Settings = @{ Keyword = 'ZEBRAS' } }
        @{ Cipher = 'Enigma'; Settings = @{ Reflector = 'C'; Rotors = 'IV', 'VI', 'VIII'; Rings = 5, 12, 26; Positions = 'QEV'; Plugboard = 'AB CD EF' } }
        @{ Cipher = 'Homophonic'; Settings = @{ A = '11', '12'; C = '13'; D = '14'; E = '15', '16', '17'; F = '18'; I = '19'; L = '20'; N = '21'; O = '22'; R = '23'; S = '24'; T = '25'; V = '26'; W = '27'; G = '28' } }
        @{ Cipher = 'MonoAlphabetic'; Settings = @{ Substitutions = 'QWERTYUIOPASDFGHJKLZXCVBNM' } }
        @{ Cipher = 'Playfair'; Settings = @{ Keyword = 'PLAYFAIREXAMPLE' } }
        @{ Cipher = 'RailFence'; Settings = @{ Rails = 3 } }
        @{ Cipher = 'Reflector'; Settings = @{ Substitutions = 'BADCFEHGJILKNMPORQTSVUXWZY' } }
        @{ Cipher = 'Rot13'; Settings = $null }
        @{ Cipher = 'Vigenere'; Settings = @{ Keyword = 'LEMON' } }
    ) {
        $plaintext = 'WEAREDISCOVERING'

        Protect-CipherText -Cipher $Cipher -Settings $Settings -Text $plaintext |
            Unprotect-CipherText -Cipher $Cipher -Settings $Settings |
            Should -BeExactly $plaintext
    }
}

Describe 'New-CipherSettings' {
    It 'makes random settings that round trip <_>' -ForEach 'Adfgvx', 'Affine', 'Autokey', 'Beaufort', 'Caesar', 'Columnar', 'Enigma', 'Homophonic', 'MonoAlphabetic', 'Playfair', 'RailFence', 'Reflector', 'Vigenere' {
        $settings = New-CipherSettings -Cipher $_ -Random

        Protect-CipherText -Cipher $_ -Settings $settings -Text 'WEAREDISCOVERING' |
            Unprotect-CipherText -Cipher $_ -Settings $settings |
            Should -BeExactly 'WEAREDISCOVERING'
    }

    It 'returns the default settings without -Random' {
        (New-CipherSettings -Cipher Caesar).RightShift | Should -Be 1
    }

    It 'refuses <_>, which has no settings to make' -ForEach 'Atbash', 'Rot13', 'Book' {
        { New-CipherSettings -Cipher $_ } | Should -Throw
    }
}

Describe 'Find-CipherKey' {
    BeforeAll {
        $script:english = 'It was a bright cold day in April and the clocks were striking thirteen. The hallway smelt of boiled ' +
            'cabbage and old rag mats. At one end of it a coloured poster, too large for indoor display, had been tacked ' +
            'to the wall. It depicted simply an enormous face, more than a metre wide: the face of a man of about forty-five, ' +
            'with a heavy black moustache and ruggedly handsome features. Winston made for the stairs. It was no use trying ' +
            'the lift. Even at the best of times it was seldom working, and at present the electric current was cut off ' +
            'during daylight hours. It was part of the economy drive in preparation for Hate Week.'
    }

    It 'cracks Caesar' {
        $result = 'YMJHFJXFWHNUMJWNXTSJTKYMJJFWQNJXYPSTBSFSIXNRUQJXYHNUMJWX' | Find-CipherKey -Cipher Caesar

        $result.Settings.RightShift | Should -Be 5
        $result.Plaintext | Should -BeExactly 'THECAESARCIPHERISONEOFTHEEARLIESTKNOWNANDSIMPLESTCIPHERS'
    }

    It 'cracks MonoAlphabetic' {
        $ciphertext = Protect-CipherText -Cipher MonoAlphabetic -Settings @{ Substitutions = 'QWERTYUIOPASDFGHJKLZXCVBNM' } -Text $english

        $result = Find-CipherKey -Cipher MonoAlphabetic -Text $ciphertext

        $result.Plaintext | Should -BeExactly $english.ToUpperInvariant()
    }

    It 'cracks only Homophonic in Italian' {
        { Find-CipherKey -Cipher Caesar -Text 'ABC' -Language Italian } | Should -Throw '*only be cracked in English*'
    }
}
