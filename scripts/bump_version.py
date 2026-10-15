#!/usr/bin/env python3
"""Sube el patch de la versión (0.1.0 -> 0.1.1) en los .csproj.
Imprime la nueva versión a stdout. Lo corre el CI en cada compilación."""
import re
import sys

FILES = ['src/MiIptv.App/MiIptv.App.csproj', 'src/MiIptv.Core/MiIptv.Core.csproj']


def main():
    nv = None
    for fp in FILES:
        with open(fp, encoding='utf-8') as f:
            t = f.read()

        def bump(m):
            nonlocal nv
            parts = m.group(1).split('.')
            parts[-1] = str(int(parts[-1]) + 1)
            nv = '.'.join(parts)
            return f'<Version>{nv}</Version>'

        t2, n = re.subn(r'<Version>([^<]*)</Version>', bump, t, count=1)
        if n == 0:
            sys.exit(f'sin <Version> en {fp}')
        with open(fp, 'w', encoding='utf-8') as f:
            f.write(t2)
    print(nv)


if __name__ == '__main__':
    sys.exit(main())
