﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace BiliDuang.DanmakuAss
{
    class DanmakuAss
    {
        /// <summary>
        /// 将Bilibili弹幕转换为ASS
        /// 作者: Kengwang
        /// </summary>
        /// <see cref="https://github.com/ikde/danmu2ass/blob/master/Danmu2Ass/PythonFile/Niconvert.py"/>
        /// <param name="xmlNodeList"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static string Convert(XmlNodeList xmlNodeList, int x, int y)
        {
            string returnstr = @"[Script Info]
; Script Generated by BiliDuang
ScriptType: v4.00+
Collisions: Normal
PlayResX: " + x.ToString() + @"
PlayResY: " + y.ToString() + @"

[V4+ Styles]
Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding
Style: BiliDuangDanmaku,Microsoft YaHei,64,&H00FFFFFF,&H00FFFFFF,&H00000000,&H00000000,0,0,0,0,100,100,0,0,1,1,0,2,20,20,20,0

[Events]
Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text
";
            List<DanmakuSingle> danmakulist = new List<DanmakuSingle>();
            try
            {
                foreach (XmlNode xmlNode in xmlNodeList)
                {
                    string text = xmlNode.InnerText;
                    string param = xmlNode.Attributes.GetNamedItem("p").InnerText;
                    string[] paramarr = param.Split(',');
                    DanmakuSingle danmaku = new DanmakuSingle
                    {
                        timeoriginal = paramarr[0],
                        type = (DanmakuType)int.Parse(paramarr[1]),
                        fontsize = int.Parse(paramarr[2]),
                        fontcolordec = int.Parse(paramarr[3]),
                        timestamp = long.Parse(paramarr[4]),
                        pool = int.Parse(paramarr[5]),
                        userid = paramarr[6],
                        rowid = long.Parse(paramarr[7]),
                        content = text
                    };
                    //采用原生的时间转换,更加稳定
                    DateTime timein = new DateTime(long.Parse((double.Parse(danmaku.timeoriginal) * 10000000).ToString()));
                    danmaku.realTime = timein;
                    danmaku.timesecond = (int)(timein.TimeOfDay.TotalSeconds);

                    danmaku.time = timein.ToString("H:mm:ss.ff");//神奇吗,要用24小时进制
                    if (danmaku.type == DanmakuType.Top || danmaku.type == DanmakuType.Bottom)
                    {
                        danmaku.timeelapse = timein.AddSeconds(5).ToString("H:mm:ss.ff");
                    }
                    else
                    {
                        danmaku.timeelapse = timein.AddSeconds(10).ToString("H:mm:ss.ff");
                    }
                    Color color = ColorTranslator.FromWin32(int.Parse(paramarr[3]));
                    danmaku.fontcolorhex = string.Format("{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);

                    danmakulist.Add(danmaku);

                }
                danmakulist = danmakulist.OrderBy(d => d.timesecond).ToList();
                Dictionary<int, int> screenrollingrow = new Dictionary<int, int>();//rolling danmaku row endtime
                Dictionary<int, int> screenstaticrow = new Dictionary<int, int>();//static danmaku row endtime
                Font msyh = new Font("Microsoft Yahei", 16f);
                foreach (DanmakuSingle danmaku in danmakulist)
                {//整理为ASS
                    string asssingle = "";
                    if (danmaku.type == DanmakuType.Bottom)
                    {
                        int nowrow = 12;
                        //底部弹幕
                        while (true)
                        {
                            if (screenstaticrow.ContainsKey(nowrow) && screenstaticrow[nowrow] + 5 > danmaku.timesecond)
                            {
                                //这行有字幕不能占有
                                nowrow--;
                                continue;
                            }
                            else
                            {
                                //可以占用此行                            
                                danmaku.y = nowrow * (y / 12);
                                screenstaticrow[nowrow] = danmaku.timesecond;
                                nowrow = 1;
                                break;
                            }
                        }
                        asssingle = "Dialogue: 4," + danmaku.time + "," + danmaku.timeelapse + ",BiliDuangDanmaku,,0,0,0,," + "{\\pos(" + x / 2 + ", " + danmaku.y + ")\\c&" + danmaku.fontcolorhex + "}" + danmaku.content + "\r\n";

                    }
                    else if (danmaku.type == DanmakuType.Top)
                    {
                        int nowrow = 1;
                        //底部弹幕
                        while (true)
                        {
                            if (screenstaticrow.ContainsKey(nowrow) && screenstaticrow[nowrow] + 5 > danmaku.timesecond)
                            {
                                //这行有字幕不能占有
                                nowrow++;
                                continue;
                            }
                            else
                            {
                                //可以占用此行                            
                                danmaku.y = nowrow * (y / 12);
                                screenstaticrow[nowrow] = danmaku.timesecond;
                                nowrow = 1;
                                break;
                            }
                        }
                        asssingle = "Dialogue: 4," + danmaku.time + "," + danmaku.timeelapse + ",BiliDuangDanmaku,,0,0,0,," + "{\\pos(" + x / 2 + ", " + danmaku.y + ")\\c&" + danmaku.fontcolorhex + "}" + danmaku.content + "\r\n";

                    }
                    else
                    {//滚动弹幕
                        int offset = (int)(new Control().CreateGraphics().MeasureString(danmaku.content, msyh).Width);
                        danmaku.startx = x + offset;
                        danmaku.endx = 0 - offset;
                        int nowrow = 1;
                        bool mid = false;
                        while (true)
                        {
                            if (screenrollingrow.ContainsKey(nowrow) && screenrollingrow[nowrow] + (System.Text.Encoding.Default.GetByteCount(danmaku.content) / 2) > danmaku.timesecond)
                            {
                                //这行有字幕不能占有
                                nowrow++;
                                continue;
                            }
                            else
                            {
                                //可以占用此行                            
                                danmaku.y = nowrow * (y / 12);
                                if (danmaku.y > y) { danmaku.y = (danmaku.y % y) + (mid ? (y / 24) : 0); mid = !mid; }
                                screenrollingrow[nowrow] = danmaku.timesecond;
                                nowrow = 1;
                                break;
                            }
                        }
                        //当前是滚动来的
                        asssingle = "Dialogue: 3," + danmaku.time + "," + danmaku.timeelapse + ",BiliDuangDanmaku,,0,0,0,," + "{\\move(" + danmaku.startx + ", " + danmaku.y + ", " + danmaku.endx + ", " + danmaku.y + ")\\c&" + danmaku.fontcolorhex + "}" + danmaku.content + "\r\n";
                    }
                    returnstr += asssingle;
                }
            }
            catch (Exception)
            {

            }

            return returnstr;
        }
    }

    enum DanmakuType
    {
        None,
        Roll,
        Roll1,
        Roll2,
        Bottom,
        Top,
        MirrorWay,
        Advanced,
        Code,
        BAS
    }

    class DanmakuSingle
    {
        //https://zhidao.baidu.com/question/1430448163912263499.html
        public string timeoriginal;//出现时间 秒为单位
        public DateTime realTime;//转换为C#的时间
        public DanmakuType type;//弹幕类型
        public int fontsize;//字体大小
        public int fontcolordec;//十进制颜色
        public long timestamp;//发送时间戳
        public int pool;//弹幕池 0普通池 1字幕池 2特殊池 【目前特殊池为高级弹幕专用】
        public string userid;//发送者识别码
        public long rowid;//
        public string content;//内容

        //ASS内容
        public int timesecond;
        public string time;//出现时间,ass格式
        public string timeelapse;//结束时间,ass格式
        public string fontcolorhex;//十六进制颜色
        public int startx, y, endx;

    }
}