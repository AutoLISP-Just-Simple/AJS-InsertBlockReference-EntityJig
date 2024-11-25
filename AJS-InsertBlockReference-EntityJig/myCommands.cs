// (C) Copyright 2024 by
//
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System.Collections.Generic;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(AJS_InsertBlockReference_EntityJig.MyCommands))]

namespace AJS_InsertBlockReference_EntityJig
{
    public class MyCommands
    {
        // Modal Command with localized name
        [CommandMethod("AJS_InsertBlock", CommandFlags.Modal)]
        public void Command_AJS_InsertBlock() // This method can have any name
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;

            if (doc == null) return;
            Editor ed = doc.Editor;
            Database db = doc.Database;

            ed.WriteMessage("\nInsertBlockReference Jig - Edited by www.lisp.vn");

            using (var tr = db.TransactionManager.StartTransaction())
            {
                var bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                int n = 1;

                Dictionary<int, ObjectId> btrs = new Dictionary<int, ObjectId>();

                ed.WriteMessage("\nSelect order of a block list: ");
                foreach (var id in bt)
                {
                    var btr = tr.GetObject(id, OpenMode.ForRead) as BlockTableRecord;
                    if (btr == null || btr.IsLayout || btr.IsAnonymous || string.IsNullOrEmpty(btr.Name)) continue;

                    btrs.Add(n, id);
                    ed.WriteMessage("\n" + n++ + "." + btr.Name);
                }

                PromptIntegerOptions pio = new PromptIntegerOptions("\nSpecify block order (from 1 to " + (n - 1) + ")");
                pio.AllowNegative = false;
                pio.AllowNone = false;

                var pir = ed.GetInteger(pio);
                if (pir.Status != PromptStatus.OK) return;

                if (pir.Value >= 1 && btrs.ContainsKey(pir.Value))
                {
                    var br = new BlockReference(Point3d.Origin, btrs[pir.Value]);
                    if (br != null)
                    {
                        if (BlockMovingRotating.Jig(br))
                        {
                            var btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                            btr.AppendEntity(br);
                            tr.AddNewlyCreatedDBObject(br, true);

                            var blockdef = tr.GetObject(br.BlockTableRecord, OpenMode.ForRead) as BlockTableRecord;
                            foreach (var atid in blockdef)
                            {
                                var attDef = tr.GetObject(atid, OpenMode.ForRead) as AttributeDefinition;
                                if (attDef != null && !attDef.Constant)
                                {
                                    var atr = new AttributeReference();
                                    atr.SetAttributeFromBlock(attDef, br.BlockTransform);
                                    atr.TextString = atr.getTextWithFieldCodes().Replace("?BlockRefId", "%<\\_ObjId " + br.ObjectId.OldIdPtr.ToString() + ">%");
                                    br.AttributeCollection.AppendAttribute(atr);
                                    tr.AddNewlyCreatedDBObject(atr, true);
                                }
                            }
                        }
                    }
                }

                tr.Commit();
            }

            ed.WriteMessage("\nInsertBlockReference Jig - Edited by www.lisp.vn");
        }
    }
}