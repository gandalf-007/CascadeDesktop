using IxMilia.Dxf.Entities;
using IxMilia.Dxf;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenTK;
using System.Windows.Media.Media3D.Converters;
using System.Data;

namespace CascadeDesktop
{
    public class DxfParser
    {
        public static LocalContour[] ConnectElements(IElement[] elems)
        {
            List<LocalContour> ret = new List<LocalContour>();

            List<Vector2d> pp = new List<Vector2d>();
            List<IElement> last = new List<IElement>();
            last.AddRange(elems);

            while (last.Any())
            {
                if (pp.Count == 0)
                {
                    pp.Add(last.First().Start);
                    pp.Add(last.First().End);
                    last.RemoveAt(0);
                }
                else
                {
                    var ll = pp.Last();
                    var f1 = last.OrderBy(z => Math.Min((z.Start - ll).Length, (z.End - ll).Length)).First();

                    var dist = Math.Min((f1.Start - ll).Length, (f1.End - ll).Length);
                    if (dist > ClosingThreshold)
                    {
                        ret.Add(new LocalContour() { Points = pp.ToList() });
                        pp.Clear();
                        continue;
                    }
                    last.Remove(f1);
                    if ((f1.Start - ll).Length < (f1.End - ll).Length)
                    {
                        pp.Add(f1.End);
                    }
                    else
                    {
                        pp.Add(f1.Start);
                    }
                }
            }
            if (pp.Any())
            {
                ret.Add(new LocalContour() { Points = pp.ToList() });
            }
            return ret.ToArray();
        }
        public static Contour[] ElementsToContours(IElement[] elems)
        {
            List<Contour> ret = new List<Contour>();

            List<IElement> pp = new List<IElement>();
            List<IElement> last = new List<IElement>();
            last.AddRange(elems);

            while (last.Any())
            {
                if (pp.Count == 0)
                {
                    pp.Add(last.First());
                    last.RemoveAt(0);
                }
                else
                {
                    var ll = pp.Last().End;
                    var f1 = last.OrderBy(z => Math.Min((z.Start - ll).Length, (z.End - ll).Length)).First();

                    var dist = Math.Min((f1.Start - ll).Length, (f1.End - ll).Length);
                    if (dist > ClosingThreshold)
                    {
                        ret.Add(new Contour() { Elements = pp.ToList() });
                        pp.Clear();
                        continue;
                    }
                    last.Remove(f1);
                    if ((f1.Start - ll).Length > (f1.End - ll).Length)
                    {
                        var clone = f1.Clone();
                        clone.Reverse();
                        pp.Add(clone);
                    }
                    else
                    {
                        pp.Add(f1);
                    }
                }
            }
            if (pp.Any())
            {
                ret.Add(new Contour() { Elements = pp.ToList() });
            }
            return ret.ToArray();
        }
        public static Vector2d Polar(Vector2d v, double angle, double distance)
        {
            return new Vector2d(v.X + Math.Cos(angle) * distance, v.Y + Math.Sin(angle) * distance);
        }
        public static double AngleTo(Vector2d v, Vector2d other)
        {
            return Math.Atan2(other.Y - v.Y, other.X - v.X);
        }
        public static double DistanceTo(Vector2d v, Vector2d other)
        {
            return Math.Sqrt((v.X - other.X) * (v.X - other.X) + (v.Y - other.Y) * (v.Y - other.Y));
        }

        public static Vector2d GetArcCenter(Vector2d startPoint, Vector2d endPoint, double bulge)
        {
            return Polar(startPoint,
                   AngleTo(startPoint, endPoint) + Math.PI / 2.0 - 2.0 * Math.Atan(bulge),
                   DistanceTo(startPoint, endPoint) * (bulge * bulge + 1.0) / 4.0 / bulge);
        }

        public static (double, double)? Bulge2IJ(double X1, double Y1, double X2, double Y2, double bulge)
        {
            double C = 0; //lunghezza della corda - length of the cord
            double H = 0; //altezza del triangolo - height of the triangle
            double alpha2 = 0; //mezzo angolo di arco  - half arc angle
            double beta = 0; //angolo della corda rispetto agli assi - orientation of the segment
                             //   List<LinhaGCode> lista = new List<LinhaGCode>();
            double I, J, R;

            if (bulge != 0)
            {
                C = Math.Sqrt(Math.Pow((X2 - X1), 2) + Math.Pow((Y2 - Y1), 2));
                alpha2 = Math.Atan(bulge) * 2;
                R = Math.Abs(C / (2 * Math.Sin(alpha2)));
                H = Math.Sqrt(Math.Pow(R, 2) - Math.Pow((C / 2), 2));

                if ((bulge > 1) || ((bulge < 0) && (bulge > -1)))
                {
                    alpha2 = alpha2 + Math.PI;
                }

                if (X1 != X2)
                {
                    beta = Math.Atan(System.Convert.ToDouble(Y2 - Y1) / System.Convert.ToDouble(X2 - X1));
                    if (X2 < X1)
                    {
                        beta = beta + Math.PI;
                    }
                }
                else
                {
                    if (Y2 < Y1)
                    {
                        beta = (-1) * (Math.PI / 2);
                    }
                    else
                    {
                        beta = Math.PI / 2; ;
                    }
                }

                if ((bulge > 1) || ((bulge < 0) && (bulge > -1)))
                {
                    I = (X2 - X1) / 2 + (Math.Cos(beta - Math.PI / 2) * H);
                    J = (Y2 - Y1) / 2 + (Math.Sin(beta - Math.PI / 2) * H);
                }
                else
                {
                    I = (X2 - X1) / 2 - (Math.Cos(beta - Math.PI / 2) * H);
                    J = (Y2 - Y1) / 2 - (Math.Sin(beta - Math.PI / 2) * H);
                }
                if (bulge > 0)
                {
                    // lista.Add(new LinhaGCode("G03", X2, Y2, config.Z_G03, I, J));
                }
                else
                {
                    //lista.Add(new LinhaGCode("G02", X2, Y2, config.Z_G03, I, J));
                }
                return (R, beta);
            }
            else
            {
                // lista.Add(new LinhaGCode("G01", X2, Y2, config.Z_G01));
            }
            //return lista;
            return null;
        }
        public static IElement[] LoadDxf(string path)
        {
            FileInfo fi = new FileInfo(path);
            DxfFile dxffile = DxfFile.Load(fi.FullName);

            IEnumerable<DxfEntity> entities = dxffile.Entities.ToArray();

            List<IElement> elems = new List<IElement>();

            foreach (DxfEntity ent in entities)
            {
                switch (ent.EntityType)
                {
                    case DxfEntityType.LwPolyline:
                        {
                            DxfLwPolyline poly = (DxfLwPolyline)ent;
                            if (poly.Vertices.Count() < 2)
                                continue;

                            if (poly.Vertices.Any(z => Math.Abs(z.Bulge) > double.Epsilon))
                            {
                                //polylines with arcs
                                PolylineElement pl = new PolylineElement();
                                List<Vector2d> pnts = new List<Vector2d>();

                                for (int i = 0; i < poly.Vertices.Count; i++)
                                {
                                    var vert = poly.Vertices[i];
                                    pnts.Add(new Vector2d(vert.X, vert.Y));
                                    if (Math.Abs(poly.Vertices[i].Bulge) > double.Epsilon)
                                    {
                                        elems.Add(new PolylineElement() { Points = pnts.ToArray() });
                                        pnts.Clear();
                                        //add arc
                                        var rr = Bulge2IJ(poly.Vertices[i].X, poly.Vertices[i].Y,
                                          poly.Vertices[(i + 1) % poly.Vertices.Count].X, poly.Vertices[(i + 1) % poly.Vertices.Count].Y, poly.Vertices[i].Bulge);

                                        var center = GetArcCenter(poly.Vertices[i].ToVector2d(),
                                            poly.Vertices[(i + 1) % poly.Vertices.Count].ToVector2d(), poly.Vertices[i].Bulge);

                                        var arc = new ArcElement(false)
                                        {
                                            Radius = rr.Value.Item1,
                                            SweepAngle = rr.Value.Item2,
                                            CCW = rr.Value.Item2 > 0,
                                            //Center
                                            Center = new Vector2d(center.X, center.Y),
                                            Start = poly.Vertices[i].ToVector2d(),
                                            End = poly.Vertices[(i + 1) % poly.Vertices.Count].ToVector2d(),
                                        };

                                        elems.Add(arc);
                                    }
                                }

                                if (pnts.Any())
                                    elems.Add(new PolylineElement() { Points = pnts.ToArray() });
                            }
                            else //simnple polyline
                            {
                                PolylineElement pl = new PolylineElement();
                                List<Vector2d> pnts = new List<Vector2d>();

                                for (int i = 0; i < poly.Vertices.Count; i++)
                                {
                                    var vert = poly.Vertices[i];

                                    pnts.Add(new Vector2d(vert.X, vert.Y));
                                }

                                elems.Add(new PolylineElement() { Points = pnts.ToArray() });
                            }
                        }
                        break;
                    case DxfEntityType.Arc:
                        {
                            DxfArc arc = (DxfArc)ent;
                            List<Vector2d> pp = new List<Vector2d>();

                            if (arc.StartAngle > arc.EndAngle)
                            {
                                arc.StartAngle -= 360;
                            }

                            for (double i = arc.StartAngle; i < arc.EndAngle; i += 15)
                            {
                                var tt = arc.GetPointFromAngle(i);
                                pp.Add(new Vector2d(tt.X, tt.Y));
                            }
                            var t = arc.GetPointFromAngle(arc.EndAngle);
                            pp.Add(new Vector2d(t.X, t.Y));
                            for (int j = 1; j < pp.Count; j++)
                            {
                                var p1 = pp[j - 1];
                                var p2 = pp[j];
                                elems.Add(new LineElement() { Start = p1, End = p2 });
                            }
                            //elems.Add(new arc)
                        }
                        break;
                    case DxfEntityType.Circle:
                        {
                            DxfCircle cr = (DxfCircle)ent;
                            //LocalContour cc = new LocalContour();

                            elems.Add(new ArcElement(true)
                            {
                                Radius = cr.Radius,                                
                                Center = cr.Center.ToVector2d(),
                                Start = cr.Center.ToVector2d() - new Vector2d(cr.Radius, 0),
                                End = cr.Center.ToVector2d() - new Vector2d(cr.Radius, 0)
                            });
                            //break;
                            //for (int i = 0; i <= 360; i += 15)
                            //{
                            //    var ang = i * Math.PI / 180f;
                            //    var xx = cr.Center.X + cr.Radius * Math.Cos(ang);
                            //    var yy = cr.Center.Y + cr.Radius * Math.Sin(ang);
                            //    cc.Points.Add(new Vector2d(xx, yy));
                            //}
                            //for (int i = 1; i < cc.Points.Count; i++)
                            //{
                            //    var p1 = cc.Points[i - 1];
                            //    var p2 = cc.Points[i];
                            //    elems.Add(new LineElement() { Start = p1, End = p2 });
                            //}
                        }
                        break;
                    case DxfEntityType.Line:
                        {
                            DxfLine poly = (DxfLine)ent;
                            elems.Add(new LineElement()
                            {
                                Start = new Vector2d(poly.P1.X, poly.P1.Y),
                                End = new Vector2d(poly.P2.X, poly.P2.Y)
                            });
                            break;
                        }

                    case DxfEntityType.Polyline:
                        {
                            DxfPolyline poly = (DxfPolyline)ent;
                            if (poly.Vertices.Count() < 2)
                                continue;

                            PolylineElement pl = new PolylineElement();
                            List<Vector2d> pnts = new List<Vector2d>();

                            for (int i = 0; i < poly.Vertices.Count; i++)
                            {
                                DxfVertex vert = poly.Vertices[i];
                                pnts.Add(new Vector2d(vert.Location.X, vert.Location.Y));
                            }

                            elems.Add(new PolylineElement() { Points = pnts.ToArray() });
                            break;
                        }
                    default:
                        throw new ArgumentException("unsupported entity type: " + ent);

                };
            }

            return elems.ToArray();
        }

        public static double RemoveThreshold = 10e-5;
        public static double ClosingThreshold = 10e-2;
    }
}
