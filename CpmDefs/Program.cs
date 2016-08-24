//
// Program.cs
//
// Author:
//       Natalia Portillo <claunia@claunia.com>
//
// Copyright (c) 2016 © Claunia.com
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Linq;

namespace CpmDefs
{
    public struct CpmDefinitions
    {
        public List<CpmDefinition> definitions;
        public DateTime creation;
    }

    public struct CpmDefinition
    {
        public string comment;
        public string encoding;
        public string bitrate;
        public int cylinders;
        public int sides;
        public int sectorsPerTrack;
        public int bytesPerSector;
        public int skew;
        public Side side1;
        public Side side2;
        public string order;
        public string label;
        public int bsh;
        public int blm;
        public int exm;
        public int dsm;
        public int drm;
        public int al0;
        public int al1;
        public int ofs;
        public int sofs;
        public bool complement;
        public bool evenOdd;
    }

    public class Side
    {
        public int sideId;
        public int[] sectorIds;
    }

    class MainClass
    {
        const string beginRegEx = "\\bBEGIN\\s*(?<label>\\w*)(\\s*(?<comment>.+))?$";
        const string informationRegEx = "^((\\s*DENSITY\\s+(?<encoding>\\w+)\\s*[,]\\s*(?<bitrate>\\w+)\\s*)?(\\s*CYLINDERS\\s(?<cylinders>\\d+)\\s*)?(\\s*SIDES\\s(?<sides>\\d+)\\s*)?(\\s*SECTORS\\s+(?<sectors>\\d+)\\s*[,]\\s*(?<bps>\\d+)\\s*)?(\\s*SKEW\\s+(?<skew>\\d+)\\s*)?(\\s*ORDER\\s+(?<order>\\w+)\\s*)?(\\s*(?<even>EVEN-ODD)\\s*)?(\\s*(?<complement>COMPLEMENT)\\s*)?(\\s*LABEL\\s+(?<label>\\w+)\\s*)?)$";
        const string sideRegEx = "\\bSIDE(?<side>[12])\\s+(?<sideid>\\d)\\s+(?<sectors>[0-9,]+)";
        const string dpbRegEx = "\\bBSH\\s+(?<bsh>\\d+)\\s*BLM\\s+(?<blm>\\d+)\\s*EXM\\s+(?<exm>\\d+)\\s*DSM\\s+(?<dsm>\\d+)\\s*DRM\\s+(?<drm>\\d+)\\s*AL0\\s+((?<al0>[0123456789ABCDEF]+)(?<hex0>H)?)\\s*AL1\\s+((?<al1>[0123456789ABCDEF]+)(?<hex1>H)?)\\s*((OFS\\s+(?<ofs>\\d+))|(SOFS\\s+(?<sofs>\\d+)))";
        const string endRegEx = "\\bEND";
        const string seeRegEx = "\\bSEE\\s*(?<label>\\w*)\\s*$";

        public static void Main(string[] args)
        {
            if(args.Length != 2)
            {
                Console.WriteLine("Usage: CpmDefs.exe <cpmdisks.noi> <cpmdisks.xml>");
                return;
            }

            StreamReader defsReader = new StreamReader(args[0]);
            bool inDefinition = false;

            CpmDefinitions defs = new CpmDefinitions();
            defs.definitions = new List<CpmDefinition>();
            int maxSectors = 0;
            int maxSize = 0;

            CpmDefinition def = new CpmDefinition();
            while(defsReader.Peek() >= 0)
            {
                string _line = defsReader.ReadLine();

                if(inDefinition)
                {
                    Regex Ir = new Regex(informationRegEx);
                    Regex Sr = new Regex(sideRegEx);
                    Regex Dr = new Regex(dpbRegEx);
                    Regex Er = new Regex(endRegEx);
                    Regex Wr = new Regex(seeRegEx);

                    Match Im;
                    Match Sm;
                    Match Dm;
                    Match Em;
                    Match Wm;

                    Im = Ir.Match(_line);
                    Sm = Sr.Match(_line);
                    Dm = Dr.Match(_line);
                    Em = Er.Match(_line);
                    Wm = Wr.Match(_line);

                    int temp;

                    if(Im.Success)
                    {
                        if(!string.IsNullOrEmpty(Im.Result("${encoding}")) && !string.IsNullOrEmpty(Im.Result("${bitrate}")))
                        {
                            def.encoding = Im.Result("${encoding}");
                            def.bitrate = Im.Result("${bitrate}");
                        }

                        if(!string.IsNullOrEmpty(Im.Result("${cylinders}")) && int.TryParse(Im.Result("${cylinders}"), out temp))
                            def.cylinders = temp;

                        if(!string.IsNullOrEmpty(Im.Result("${sides}")) && int.TryParse(Im.Result("${sides}"), out temp))
                            def.sides = temp;

                        if(!string.IsNullOrEmpty(Im.Result("${skew}")) && int.TryParse(Im.Result("${skew}"), out temp))
                            def.skew = temp;

                        if(!string.IsNullOrEmpty(Im.Result("${sectors}")) && !string.IsNullOrEmpty(Im.Result("${bps}")))
                        {
                            if(int.TryParse(Im.Result("${sectors}"), out temp))
                                def.sectorsPerTrack = temp;
                            if(int.TryParse(Im.Result("${bps}"), out temp))
                                def.bytesPerSector = temp;
                        }

                        if(!string.IsNullOrEmpty(Im.Result("${order}")))
                            def.order = Im.Result("${order}");

                        def.evenOdd |= (!string.IsNullOrEmpty(Im.Result("${even}")) && Im.Result("${even}") == "EVEN-ODD");
                        def.complement |= (!string.IsNullOrEmpty(Im.Result("${complement}")) && Im.Result("${complement}") == "COMPLEMENT");

                        if(!string.IsNullOrEmpty(Im.Result("${label}")))
                            def.label = Im.Result("${label}");
                    }
                    else if(Sm.Success)
                    {
                        if(Sm.Result("${side}") == "1" && int.TryParse(Sm.Result("${sideid}"), out temp))
                        {
                            def.side1 = new Side();
                            def.side1.sideId = temp;
                            string[] sectorIds = Sm.Result("${sectors}").Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                            def.side1.sectorIds = new int[sectorIds.Length];
                            for(int i = 0; i < sectorIds.Length; i++)
                            {
                                string toConvert;
                                int fromBase;

                                if(sectorIds[i].ToLowerInvariant().Last() == 'h')
                                {
                                    toConvert = sectorIds[i].Substring(0, sectorIds[i].Length - 1);
                                    fromBase = 16;
                                }
                                else
                                {
                                    toConvert = sectorIds[i];
                                    fromBase = 10;
                                }

                                def.side1.sectorIds[i] = Convert.ToInt32(toConvert, fromBase);
                            }
                        }
                        else if(Sm.Result("${side}") == "2" && int.TryParse(Sm.Result("${sideid}"), out temp))
                        {
                            def.side2 = new Side();
                            def.side2.sideId = temp;
                            string[] sectorIds = Sm.Result("${sectors}").Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                            def.side2.sectorIds = new int[sectorIds.Length];
                            for(int i = 0; i < sectorIds.Length; i++)
                            {
                                string toConvert;
                                int fromBase;

                                if(sectorIds[i].ToLowerInvariant().Last() == 'h')
                                {
                                    toConvert = sectorIds[i].Substring(0, sectorIds[i].Length - 1);
                                    fromBase = 16;
                                }
                                else
                                {
                                    toConvert = sectorIds[i];
                                    fromBase = 10;
                                }

                                def.side2.sectorIds[i] = Convert.ToInt32(toConvert, fromBase);
                            }
                        }
                    }
                    else if(Dm.Success)
                    {
                        if(int.TryParse(Dm.Result("${bsh}"), out temp))
                            def.bsh = temp;
                        if(int.TryParse(Dm.Result("${blm}"), out temp))
                            def.blm = temp;
                        if(int.TryParse(Dm.Result("${exm}"), out temp))
                            def.exm = temp;
                        if(int.TryParse(Dm.Result("${dsm}"), out temp))
                            def.dsm = temp;
                        if(int.TryParse(Dm.Result("${drm}"), out temp))
                            def.drm = temp;
                        if(Dm.Result("${hex0}") == "H")
                            def.al0 = Convert.ToInt32(Dm.Result("${al0}"), 16);
                        else
                            def.al0 = Convert.ToInt32(Dm.Result("${al0}"), 10);
                        if(Dm.Result("${hex1}") == "H")
                            def.al1 = Convert.ToInt32(Dm.Result("${al1}"), 16);
                        else
                            def.al1 = Convert.ToInt32(Dm.Result("${al1}"), 10);
                        if(int.TryParse(Dm.Result("${ofs}"), out temp))
                            def.ofs = temp;
                        if(int.TryParse(Dm.Result("${sofs}"), out temp))
                            def.sofs = temp;
                    }
                    else if(Em.Success)
                    {
                        defs.definitions.Add(def);
                        inDefinition = false;

                        int sectors = def.cylinders * def.sides * def.sectorsPerTrack;
                        int size = sectors * def.bytesPerSector;

                        if(sectors > maxSectors)
                            maxSectors = sectors;
                        if(size > maxSize)
                            maxSize = size;
                    }
                    else inDefinition &= !Wm.Success;
                }
                else
                {
                    Regex Br = new Regex(beginRegEx);
                    Match Bm;
                    Bm = Br.Match(_line);

                    if(Bm.Success == true)
                    {
                        inDefinition = true;
                        def = new CpmDefinition();
                        if(!string.IsNullOrEmpty(Bm.Result("${label}")))
                           def.label = Bm.Result("${label}");
                        if(!string.IsNullOrEmpty(Bm.Result("${comment}")))
                           def.comment = Bm.Result("${comment}");
                    }
                }
            }

            defsReader.Close();

            Console.WriteLine("Converted {0} definitions", defs.definitions.Count);
            Console.WriteLine("Disk with most sectors had {0} sectors", maxSectors);
            Console.WriteLine("Biggest disk had {0} bytes", maxSize);

            Console.WriteLine("Removing duplicate definitions");
            CpmDefinitions noDups = new CpmDefinitions();
            noDups.definitions = new List<CpmDefinition>();

            foreach(CpmDefinition oldDef in defs.definitions)
            {
                bool unique = true;

                foreach(CpmDefinition newDef in noDups.definitions)
                {
                    bool sameEncoding = oldDef.encoding == newDef.encoding;
                    bool sameBitrate = oldDef.bitrate == newDef.bitrate;
                    bool sameCylinders = oldDef.cylinders == newDef.cylinders;
                    bool sameSides = oldDef.sides == newDef.sides;
                    bool sameSectorsPerTrack = oldDef.sectorsPerTrack == newDef.sectorsPerTrack;
                    bool sameBytesPerSector = oldDef.bytesPerSector == newDef.bytesPerSector;
                    bool sameSkew = oldDef.skew == newDef.skew;
                    bool sameOrder = oldDef.order == newDef.order;
                    bool sameBsh = oldDef.bsh == newDef.bsh;
                    bool sameBlm = oldDef.blm == newDef.blm;
                    bool sameExm = oldDef.exm == newDef.exm;
                    bool sameDsm = oldDef.dsm == newDef.dsm;
                    bool sameDrm = oldDef.drm == newDef.drm;
                    bool sameAl0 = oldDef.al0 == newDef.al0;
                    bool sameAl1 = oldDef.al1 == newDef.al1;
                    bool sameOfs = oldDef.ofs == newDef.ofs;
                    bool sameSofs = oldDef.sofs == newDef.sofs;
                    bool sameComplement = oldDef.complement == newDef.complement;
                    bool sameEvenOdd = oldDef.evenOdd == newDef.evenOdd;

                    bool sameSide1;
                    if(oldDef.side1 == null && newDef.side1 == null)
                        sameSide1 = true;
                    else if(oldDef.side1 != null && newDef.side1 != null)
                    {
                        if(oldDef.side1.sideId != newDef.side1.sideId)
                            sameSide1 = false;
                        else
                            sameSide1 = oldDef.side1.sectorIds.SequenceEqual(newDef.side1.sectorIds);
                    }
                    else
                        sameSide1 = false;

                    bool sameSide2;
                    if(oldDef.side2 == null && newDef.side2 == null)
                        sameSide2 = true;
                    else if(oldDef.side2 != null && newDef.side2 != null)
                    {
                        if(oldDef.side2.sideId != newDef.side2.sideId)
                            sameSide2 = false;
                        else
                            sameSide2 = oldDef.side2.sectorIds.SequenceEqual(newDef.side2.sectorIds);
                    }
                    else
                        sameSide2 = false;

                    if(sameEncoding && sameBitrate && sameCylinders && sameSides && sameSectorsPerTrack && sameBytesPerSector && sameSkew && sameOrder &&
                       sameBsh && sameBlm && sameExm && sameDsm && sameDrm && sameAl0 && sameAl1 && sameOfs && sameSofs && sameComplement && sameEvenOdd &&
                       sameSide1 && sameSide2)
                    {
                        unique = false;
                        break;
                    }
                }

                if(unique)
                    noDups.definitions.Add(oldDef);
            }

            defs = noDups;
            Console.WriteLine("Writing {0} definitions", defs.definitions.Count);
            defs.creation = DateTime.UtcNow;
            TextWriter defsWriter = new StreamWriter(args[1]);
            XmlSerializer ser = new XmlSerializer(typeof(CpmDefinitions));
            ser.Serialize(defsWriter, defs);
            defsWriter.Close();

        }
    }
}
