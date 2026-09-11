"""Lexical delimiter check for this C# 7.3 tree, NOT a compiler/type checker.

Skips comments, chars, ordinary/verbatim strings. Interpolated strings need a
real C# parser and are explicitly rejected instead of silently mis-scanned.
"""
from pathlib import Path
import sys


def check_source(source, name='<source>'):
    stack = []
    pairs = {')': '(', ']': '[', '}': '{'}
    i, line = 0, 1
    while i < len(source):
        c = source[i]
        if c == '\n':
            line += 1
            i += 1
            continue
        if source.startswith('//', i):
            end = source.find('\n', i + 2)
            i = len(source) if end < 0 else end
            continue
        if source.startswith('/*', i):
            end = source.find('*/', i + 2)
            if end < 0:
                raise ValueError(f'{name}:{line}: unclosed block comment')
            line += source[i:end + 2].count('\n')
            i = end + 2
            continue
        if any(source.startswith(prefix, i) for prefix in ('$"', '$@"', '@$"')):
            raise ValueError(f'{name}:{line}: interpolated string requires C# parser validation')
        verbatim = source.startswith('@"', i)
        if verbatim or c in ('"', "'"):
            quote = '"' if verbatim else c
            start_line = line
            i += 2 if verbatim else 1
            closed = False
            while i < len(source):
                if source[i] == '\n':
                    if not verbatim:
                        raise ValueError(f'{name}:{start_line}: newline in ordinary string/char literal')
                    line += 1
                if not verbatim and source[i] == '\\':
                    i += 2
                    continue
                if source[i] == quote:
                    if verbatim and i + 1 < len(source) and source[i + 1] == quote:
                        i += 2
                        continue
                    i += 1
                    closed = True
                    break
                i += 1
            if not closed:
                raise ValueError(f'{name}:{start_line}: unclosed literal')
            continue
        if c in '([{':
            stack.append((c, line))
        elif c in ')]}':
            if not stack or stack[-1][0] != pairs[c]:
                raise ValueError(f'{name}:{line}: unmatched {c}')
            stack.pop()
        i += 1
    if stack:
        c, start_line = stack[-1]
        raise ValueError(f'{name}:{start_line}: unclosed {c}')


def check_tree(root):
    paths = sorted((root / 'src/SolidWorksAddin').rglob('*.cs')) + sorted((root / 'tests').glob('*.cs'))
    paths = [p for p in paths if not {'bin', 'obj'}.intersection(p.parts)]
    for path in paths:
        check_source(path.read_text(encoding='utf-8-sig'), str(path.relative_to(root)))
    return len(paths)


if __name__ == '__main__':
    root = Path(__file__).resolve().parents[1]
    try:
        count = check_tree(root)
    except ValueError as error:
        print(f'FAIL: {error}', file=sys.stderr)
        sys.exit(1)
    print(f'PASS: lexical delimiter balance in {count} C# files. C# compilation is still required.')
