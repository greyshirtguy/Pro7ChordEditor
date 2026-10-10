from os import chdir, system
import re

VERSION_RE = r'\d+(?:\.\d+){0,3}(?:-.+?)?'
ASSEMBLYINFO_PATH = r'PRo7ChordEditor\AssemblyInfo.cs'
PUBLISH_PATH = r'PRo7ChordEditor\bin\Release\net8.0-windows\_publish'

def get_curr_ver() -> str | None:
    try:
        with open(ASSEMBLYINFO_PATH) as file:
            data = file.read()
            ver = re.search(fr'\[assembly: AssemblyVersion\("({VERSION_RE})"\)\]', data).group(1)
            return ver
    except FileNotFoundError:
        print('\x1b[33;1mWARNING\x1b[0m: AssemblyInfo.cs not found')
        return None
    except Exception as e:
        print(f'\x1b[33;1mWARNING\x1b[0m: Unable to read the current version from AssemblyInfo.cs:\n  {e}')
        return None

def suggest_next_ver(curr_ver: str) -> str:
    ver = curr_ver.split('-')[0].split('.')
    suggestions = []
    for i in range(len(ver)):
        s = ver.copy()
        s[i] = str(int(s[i]) + 1)
        for j in range(i + 1, len(ver)):
            s[j] = '0'
        while len(s) < 3:
            s.append('0')
        if len(s) == 4 and s[3] == '0':
            s.pop()
        suggestions.append('.'.join(s))
    suggestions.reverse()
    return suggestions

def main():
    print('==== \x1b[32mPro7 Chord Editor publish tool\x1b[0m ====')
    curr_ver = get_curr_ver()
    
    if curr_ver:
        print(f'Current version of the program is \x1b[4m{curr_ver}\x1b[0m')
        suggestions = suggest_next_ver(curr_ver)
        print('\x1b[2mSuggested new versions:\x1b[0m')
        for s in suggestions:
            print(f'  \x1b[2m{s}\x1b[0m')

    new_ver: str = input('New version: ')

    if new_ver not in suggestions:
        print('\x1b[33;1mWARNING\x1b[0m: Not a sequential version')
        print('Do you want to continue? (y/N) ', end='')
        c = input().strip().lower()
        if c != 'y':
            print('Aborted')
            return

    with open(ASSEMBLYINFO_PATH) as file:
        data = file.read()
        data = re.sub(fr'(\[assembly: AssemblyVersion\()"{VERSION_RE}"(\)\])', fr'\1"{new_ver}"\2', data)

    with open(ASSEMBLYINFO_PATH, 'w') as file:
        file.write(data)

    print(f'Version updated to \x1b[4m{new_ver}\x1b[0m')

    print('Are you sure you want to continue? (y/N) ', end='')
    c = input().strip().lower()
    if c != 'y':
        print('Aborted')
        return

    print('==== \x1b[32mCommitting...\x1b[0m ====')

    system(r'git add PRo7ChordEditor\AssemblyInfo.cs')
    system(f'git commit -m "Version {new_ver}"')
    system('git push')
    system(f'git tag -a v{new_ver} -m "Version {new_ver}"')
    system('git push --tags')

    print('==== \x1b[32mPublishing...\x1b[0m ====')

    system('dotnet clean')

    system('dotnet publish -p:PublishProfile=FrameworkDependent')
    system('dotnet publish -p:PublishProfile=SelfContained')
    
    print('==== \x1b[32mCompressing...\x1b[0m ====')

    chdir(fr'{PUBLISH_PATH}\FrameworkDependent')
    system(fr'tar -acvf ..\PRo7ChordEditor-{new_ver}-FD.zip *')
    chdir(fr'..\SelfContained')
    system(fr'tar -acvf ..\PRo7ChordEditor-{new_ver}-SC.zip *')
 
    print('==== \x1b[32mDone\x1b[0m ====')

if __name__ == '__main__':
    main()
