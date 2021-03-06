﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using SaNi.Spriter.Data;
using Curve = SaNi.Spriter.Data.Curve;
using Point = SaNi.Spriter.Data.Point;

namespace SaNi.Spriter
{
    using TimelineKey = Timeline.Key;
    public class SpriterReader : ContentTypeReader<SpriterData>
    {
        private SpriterData data;
        protected override SpriterData Read(ContentReader input, SpriterData data)
        {
            
            string scmlversion = input.ReadString();
            string generator = input.ReadString();
            string generatorVersion = input.ReadString();
            int folderCount = input.ReadInt32();
            int entityCount = input.ReadInt32();
            this.data = new SpriterData(scmlversion, generator, generatorVersion, folderCount, entityCount);
            data = this.data;
            /*int[] files = new int[folderCount];

            for (int i = 0; i < folderCount; i++)
            {
                // filujen määrä per folder
                files[i] = input.ReadInt32();
            }*/
            LoadFolders(input, data, folderCount);
            LoadEntities(input, data, entityCount);

            return data;
        }

        #region Folders and files

        private void LoadFolders(ContentReader input, SpriterData data, int folderCount)
        {
            for (int folder = 0; folder < folderCount; folder++)
            {
                string name = input.ReadString();
                // folder on id
                int count = input.ReadInt32();
                data.AddFolder(new Folder(folder, name, count));
                LoadFiles(input, data.GetFolder(folder), count);
            }
        }

        private void LoadFiles(ContentReader input, Folder folder, int fileCount)
        {
            for (int file = 0; file < fileCount; file++)
            {
                // file on id
                string name = input.ReadString();
                int w = input.ReadInt32();
                int h = input.ReadInt32();
                float pivotX = input.ReadSingle();
                float pivotY = input.ReadSingle();
                folder.AddFile(new File(file, name, new Dimension(w, h), new Point(pivotX, pivotY)));
            }
        }

        #endregion


        #region Entities

        private void LoadEntities(ContentReader input, SpriterData data, int entityCount)
        {
            for (int i = 0; i < entityCount; i++)
            {
                // i on id
                string name = input.ReadString();
                int objInfoCount = input.ReadInt32();
                int charMapCount = input.ReadInt32();
                int animationCount = input.ReadInt32();
                Entity entity = new Entity(i, name, animationCount, charMapCount, objInfoCount);
                data.AddEntity(entity);
                // TODO lataa object infot, charmapit ja animaatiot
                LoadObjectInfos(input, entity, objInfoCount);
                LoadCharacterMaps(input, entity, charMapCount);
                LoadAnimations(input, entity, animationCount);
            }
        }

        #region Object infos

        private void LoadObjectInfos(ContentReader input, Entity entity, int count)
        {
            for (int i = 0; i < count; i++)
            {
                string name = input.ReadString();
                string type = input.ReadString();
                int w = input.ReadInt32();
                int h = input.ReadInt32();
                Entity.ObjectInfo info = new Entity.ObjectInfo(name, SpriterUtils.GetObjectInfoFor(type), new Dimension(w, h), new List<FileReference>());
                entity.AddInfo(info);
                // todo frameja? ei näy dokkarissa
            }
        }

        #endregion

        #region Character maps

        private void LoadCharacterMaps(ContentReader input, Entity entity, int charMapCount)
        {
            for (int i = 0; i < charMapCount; i++)
            {
                // i on id
                string name = input.ReadString();
                Entity.CharacterMap map = new Entity.CharacterMap(i, name);


                int mapCount = input.ReadInt32();
                LoadMaps(input, map, mapCount);


                entity.AddCharacterMap(map);
            }
        }

        private void LoadMaps(ContentReader input, Entity.CharacterMap map, int count)
        {
            for (int j = 0; j < count; j++)
            {
                int folder = input.ReadInt32();
                int file = input.ReadInt32();
                int targetFolder = input.ReadInt32();
                int targetFile = input.ReadInt32();
                map.Add(new FileReference(folder, file), new FileReference(targetFolder, targetFile));
            }
        }


        #endregion

        #region Animations

        private void LoadAnimations(ContentReader input, Entity entity, int animationCount)
        {
            for (int i = 0; i < animationCount; i++)
            {
                // i on id
                string name = input.ReadString();
                int length = input.ReadInt32();
                bool looping = input.ReadBoolean();

                int mainlineKeysCount = input.ReadInt32();
                int timelineCount = input.ReadInt32();
                SpriterAnimation anim = new SpriterAnimation(
                    new Mainline(mainlineKeysCount),
                    i,
                    name,
                    length,
                    looping,
                    timelineCount
                    );
                entity.AddAnimation(anim);
                
                LoadMainlineKeys(input, anim.Mainline, mainlineKeysCount);
                LoadTimelines(input, anim, entity, timelineCount);
                anim.Prepare();
            }
        }

        private void LoadTimelines(ContentReader input, SpriterAnimation anim, Entity entity, int count)
        {
            for (int id = 0; id < count; id++)
            {
                string name = input.ReadString();
                string objtype = input.ReadString();
                int keyCount = input.ReadInt32();
                Entity.ObjectType type = SpriterUtils.GetObjectInfoFor(objtype);
                Entity.ObjectInfo info = entity.GetInfo(name);
                if (info == null) info = new Entity.ObjectInfo(name, type, new Dimension(0f,0f), new List<FileReference>());
                Timeline timeline = new Timeline(id, name, info, keyCount);
                anim.AddTimeline(timeline);
                LoadTimelineKeys(input, timeline, keyCount);
            }
        }

        private void LoadTimelineKeys(ContentReader input, Timeline timeline, int count)
        {
            for (int id = 0; id < count; id++)
            {
                int spin = input.ReadInt32();
                int time = input.ReadInt32();
                string curveType = input.ReadString();
                float[] c = new[]
                {
                    input.ReadSingle(),
                    input.ReadSingle(),
                    input.ReadSingle(),
                    input.ReadSingle()
                };
                Curve curve = new Curve();
                curve.Type = SpriterUtils.GetType(curveType);
                curve.Constraints.Set(c[0],c[1], c[2], c[3]);
                TimelineKey key = new TimelineKey(id, time, spin, curve);
                LoadObjectOrBone(input, key, timeline);
            }
        }

        private void LoadObjectOrBone(ContentReader input, TimelineKey key, Timeline timeline)
        {
            string name = input.ReadString();
            Point pivot = new Point(input.ReadSingle(), input.ReadSingle());
            Point scale = new Point(input.ReadSingle(), input.ReadSingle());
            Point position = new Point(input.ReadSingle(), input.ReadSingle());
            float angle = input.ReadSingle();
            int folder = -1, file = -1;
            float alpha = 1f;
            if (name == "object")
            {
                if (timeline.ObjectInfo.Type == Entity.ObjectType.Sprite)
                {
                    folder = input.ReadInt32();
                    file = input.ReadInt32();
                    alpha = input.ReadSingle();
                    
                    
                    File f = data.GetFolder(folder).GetFile(file);
                    // TODO objectilla voi olla kans pivot, pitäs kirjottaa?
                    pivot = new Point(f.Pivot.X, f.Pivot.Y);
                    timeline.ObjectInfo.Size.Set(f.Size);


                }
            }
            SpriterObject obj;
            if (name == "bone")
            {
                obj = new SpriterObject(position, scale, pivot, angle, alpha, new FileReference(folder, file));
            }
            else
            {
                obj = new SpriterObject(position, scale, pivot, angle, alpha, new FileReference(folder, file));
            }
            key.Object = obj;
            timeline.AddKey(key);
        }

        private void LoadMainlineKeys(ContentReader input, Mainline mainline, int count)
        {
            for (int id = 0; id < count; id++)
            {
                int time = input.ReadInt32();
                int objectRefCount = input.ReadInt32();
                int boneRefCount = input.ReadInt32();
                string curveType = input.ReadString();
                float[] cs = new[]
                {
                    input.ReadSingle(),
                    input.ReadSingle(),
                    input.ReadSingle(),
                    input.ReadSingle(),
                };
                Curve curve = new Curve();
                curve.Type = SpriterUtils.GetType(curveType);
                curve.Constraints.Set(cs[0], cs[1], cs[2], cs[3]);

                Mainline.Key key = new Mainline.Key(id, time, curve, boneRefCount, objectRefCount);
                mainline.AddKey(key);
                LoadRefs(input, objectRefCount, boneRefCount, key);
            }

        }

        private void LoadRefs(ContentReader input, int objectRefCount, int boneRefCount, Mainline.Key _key)
        {
            for (int id = 0; id < boneRefCount; id++)
            {
                int parent = input.ReadInt32();
                int timeline = input.ReadInt32();
                int key = input.ReadInt32();
                
                Mainline.Key.BoneRef boneRef = new Mainline.Key.BoneRef(id, timeline, key, _key.GetBoneRef(parent));
                _key.AddBoneRef(boneRef);
            }

            for (int id = 0; id < objectRefCount; id++)
            {
                int parent = input.ReadInt32();
                int timeline = input.ReadInt32();
                int key = input.ReadInt32();
                int z = input.ReadInt32();
                Mainline.Key.ObjectRef objref = new Mainline.Key.ObjectRef(id, timeline, key, _key.GetBoneRef(parent), z);
                _key.AddObjectRef(objref);
            }

            // TODO sort?
        }

        #endregion

        #endregion

    }
}
