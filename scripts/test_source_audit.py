import unittest
from check_csharp_delimiters import check_source


class DelimiterTests(unittest.TestCase):
    def test_missing_namespace_closure(self):
        with self.assertRaisesRegex(ValueError, 'unclosed'):
            check_source('namespace Mechra { class Welcome { void Create() { } }')

    def test_comments_and_literals_do_not_close_namespace(self):
        check_source('namespace Mechra { // }\n /* [ ) */ class A { string s = "} ] )"; char c = \'{\'; } }')

    def test_verbatim_paths_quotes_and_newlines(self):
        check_source('class A { string s = @"C:\\Mechra\\{\n""quoted"""; }')

    def test_wrong_order_and_unclosed_literals(self):
        for code in ('class A { void B(] {} }', 'class A { string s = "oops;', 'class A { /* oops'):
            with self.subTest(code=code), self.assertRaises(ValueError):
                check_source(code)

    def test_interpolation_requires_real_parser(self):
        with self.assertRaisesRegex(ValueError, 'requires C# parser'):
            check_source('class A { string s = $"{value}"; }')


if __name__ == '__main__':
    unittest.main()
