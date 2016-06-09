using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Takai
{
    /// <summary>
    /// A database of localized strings
    /// </summary>
    public class Locale
    {
        /// <summary>
        /// All of the strings (common string, language)
        /// </summary>
        public System.Collections.Generic.Dictionary<string, string[]> strings;

        /// <summary>
        /// The current language being used
        /// If language is not in languages list, value not changed
        /// </summary>
        public string currentLanguage
        {
            get { return languages[_cLang]; }
            set
            {
                _cLang = FindLang(value);
            }
        }
        int _cLang;

        /// <summary>
        /// All of the available translation languages
        /// </summary>
        public string[] languages;

        /// <summary>
        /// Create an empty locale
        /// </summary>
        public Locale()
        {
            strings = new System.Collections.Generic.Dictionary<string, string[]>(0);
            _cLang = 0;
            languages = new string[0];
        }

        /// <summary>
        /// Load a language from a .lang file
        /// </summary>
        /// <param name="File">The .lang file to use</param>
        /// <returns>A loaded locale</returns>
        public static Locale FromFile(string File)
        {
            return FromStream(new System.IO.FileStream(File, System.IO.FileMode.Open));
        }

        /// <summary>
        /// Load a language file from a stream
        /// </summary>
        /// <param name="stream">The stream to load the data from</param>
        /// <returns>The loaded locale</returns>
        public static Locale FromStream(System.IO.Stream Stream)
        {
            Locale l = new Locale();

            //load
            System.IO.StreamReader filer = new System.IO.StreamReader(Stream);
            int ln = -1;
            int cLang = -1;
            int cName = -1;
            int strCt = 0;

            while (!filer.EndOfStream)
            {
                string[] line = filer.ReadLine().Split('=');

                if (line[0][0] == '#') //first line not important & comments ignored
                    continue;

                ln++;

                if (ln == 0)
                    continue;
                else if (ln == 1) //number of languages
                    l.languages = new string[int.Parse(line[1])];
                else if (ln == 2) //number of strings
                {
                    strCt = int.Parse(line[1]);
                }

                else //read rest of file
                {
                    if (line[0][0] == '[') //new language
                    {
                        cLang++;
                        l.languages[cLang] = line[0].Substring(1, line[0].Length - 2);
                        cName = -1;
                    }
                    else //load strings
                    {
                        cName++;

                        if (cLang == 0) //fill common names list on first language
                            l.strings.Add(line[0], new string[l.languages.Length]);

                        l.strings[line[0]][cLang] = line[1];
                    }
                }
            }

            filer.Close();

            return l;
        }

        /// <summary>
        /// Load a .Net Resource file
        /// </summary>
        /// <param name="file">The .resx to load</param>
        /// <returns>The loaded locale</returns>
        public static Locale FromResx(string Resx)
        {
            return FromResx(new System.IO.FileStream(Resx, System.IO.FileMode.Open));
        }

        /// <summary>
        /// Load a .Net resource file
        /// </summary>
        /// <param name="stream">The stream to load</param>
        /// <returns>The loaded locale</returns>
        public static Locale FromResx(System.IO.Stream Stream)
        {
            Locale l = new Locale();



            return l;
        }

        /// <summary>
        /// Get a localized string based on the current locale (unless language is overridden)
        /// </summary>
        /// <param name="commonName">The common string to retrieve</param>
        /// <param name="language">An optional language override (returns empty string if language doesn't exist)</param>
        /// <returns>The localized string</returns>
        public string GetLocalizedString(string commonName, string language)
        {
            int fnd = language != "" ? FindLang(language) : _cLang;
            if (fnd > -1)
            {
                string[] val;
                if (strings.TryGetValue(commonName, out val))
                    return val[fnd];
            }
            return string.Empty;
        }

        /// <summary>
        /// Get a localized string in the current culture
        /// </summary>
        /// <param name="String">The common name string to get</param>
        /// <returns>The string if it exists, an empty string otherwise</returns>
        public string this[string String]
        {
            get
            {
                return GetLocalizedString(String, System.Globalization.CultureInfo.CurrentCulture.Name);
            }
        }

        /// <summary>
        /// Get the language index based on a string
        /// </summary>
        /// <param name="language">The language string to search</param>
        /// <returns>The index in the languages array</returns>
        int FindLang(string language)
        {
            int lng = _cLang;

            for (int i = 0; i < languages.Length; i++)
                if (languages[i] == language)
                {
                    lng = i;
                    break;
                }

            return lng;
        }
    }
}
