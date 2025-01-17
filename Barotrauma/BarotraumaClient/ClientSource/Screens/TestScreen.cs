#nullable enable
using System.Linq;
using Barotrauma.Extensions;
using Barotrauma.Items.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/*
 * This screen only exists because I'm going mental without access to EnC on Linux.
 * This is fucking stupid and horrible.
 * Remember to remove this crap eventually.
 * - Markus
 */
namespace Barotrauma
{
    internal sealed class TestScreen : EditorScreen
    {
        public override Camera Cam { get; }

        private Item? miniMapItem;

        public static Character? dummyCharacter;
        public static Effect? BlueprintEffect;

        public TestScreen()
        {
            Cam = new Camera();
            BlueprintEffect = GameMain.GameScreen.BlueprintEffect;

            new GUIButton(new RectTransform(new Point(256, 256), Frame.RectTransform), "Reload shader")
            {
                OnClicked = (button, o) =>
                {
                    BlueprintEffect.Dispose();
                    GameMain.Instance.Content.Unload();
                    BlueprintEffect = GameMain.Instance.Content.Load<Effect>("Effects/blueprintshader_opengl");
                    GameMain.GameScreen.BlueprintEffect = BlueprintEffect;
                    return true;
                }
            };
        }

        public override void Select()
        {
            base.Select();
            if (dummyCharacter is { Removed: false })
            {
                dummyCharacter?.Remove();
            }

            dummyCharacter = Character.Create(CharacterPrefab.HumanSpeciesName, Vector2.Zero, "", id: Entity.DummyID, hasAi: false);
            dummyCharacter.Info.Job = new Job(JobPrefab.Prefabs.Where(jp => TalentTree.JobTalentTrees.ContainsKey(jp.Identifier)).GetRandom(Rand.RandSync.Unsynced));
            dummyCharacter.Info.Name = "Galldren";
            dummyCharacter.Inventory.CreateSlots();

            miniMapItem = new Item(ItemPrefab.Find(null, "deconstructor".ToIdentifier()), Vector2.Zero, null, 1337, false);

            foreach (ItemComponent component in miniMapItem.Components)
            {
                component.OnItemLoaded();
            }
            Character.Controlled = dummyCharacter;
            GameMain.World.ProcessChanges();
        }

        public override void AddToGUIUpdateList()
        {
            Frame.AddToGUIUpdateList();
            CharacterHUD.AddToGUIUpdateList(dummyCharacter);
            dummyCharacter?.SelectedItem?.AddToGUIUpdateList();
        }

        public override void Update(double deltaTime)
        {
            base.Update(deltaTime);

            if (dummyCharacter is { } dummy && miniMapItem is { } item)
            {
                if (dummy.SelectedItem != item)
                {
                    dummy.SelectedItem = item;
                }

                dummy.SelectedItem?.UpdateHUD(Cam, dummy, (float)deltaTime);
                Vector2 pos = FarseerPhysics.ConvertUnits.ToSimUnits(item.Position);

                foreach (Limb limb in dummy.AnimController.Limbs)
                {
                    limb.body.SetTransform(pos, 0.0f);
                }

                if (dummy.AnimController?.Collider is { } collider)
                {
                    collider.SetTransform(pos, 0);
                }

                dummy.ControlLocalPlayer((float)deltaTime, Cam, false);
                dummy.Control((float)deltaTime, Cam);
            }
        }

        public override void Draw(double deltaTime, GraphicsDevice graphics, SpriteBatch spriteBatch)
        {
            base.Draw(deltaTime, graphics, spriteBatch);
            graphics.Clear(BackgroundColor);

            spriteBatch.Begin(SpriteSortMode.BackToFront, transformMatrix: Cam.Transform);
            miniMapItem?.Draw(spriteBatch, false);
            if (dummyCharacter is { } dummy)
            {
                dummyCharacter.DrawFront(spriteBatch, Cam);
                dummyCharacter.Draw(spriteBatch, Cam);
            }

            spriteBatch.End();

            spriteBatch.Begin(SpriteSortMode.Deferred, samplerState: GUI.SamplerState);

            GUI.Draw(Cam, spriteBatch);

            dummyCharacter?.DrawHUD(spriteBatch, Cam, false);

            spriteBatch.End();
        }
    }
}