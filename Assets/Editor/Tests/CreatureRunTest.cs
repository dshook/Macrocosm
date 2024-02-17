using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Tests
{
    public class CreatureRunTest
    {
        [Test]
        public void Simple()
        {
            var a = new CreatureData(){
                speed = 2,
            };
            var b = new CreatureData(){
                speed = 1,
            };
            var c = new CreatureData(){
                speed = 1,
            };

            Assert.True(CreatureData.CreatureCanRun(b, a), "A can run from B");
            Assert.False(CreatureData.CreatureCanRun(a, b), "B can't run from A");
            Assert.False(CreatureData.CreatureCanRun(b, c), "C can't run from B");
        }

        [Test]
        public void Ambush()
        {
            var a = new CreatureData(){
                speed = 2,
                power = 1,
            };
            var b = new CreatureData(){
                speed = 1,
                power = 3,
                modifiers = new List<CreatureModifierId>(){ CreatureModifierId.Ambush }
            };
            var c = new CreatureData(){
                speed = 3,
                power = 1,
            };

            Assert.False(CreatureData.CreatureCanRun(b, a), "A can't run from B");
            Assert.False(CreatureData.CreatureCanRun(a, b), "B can't run from A");
            Assert.True(CreatureData.CreatureCanRun(b, c), "C can run from B");
        }
    }

    public class CreatureUpgradeTest
    {
        [Test]
        public void PersistentMods()
        {
            var stageFourData = new StageFourDataModel(){
                totalChildCount = 1
            };

            var data1 = new CreatureData(){
                speed = 2,
                modifiers = new List<CreatureModifierId>(){
                    CreatureModifierId.Omnivore,
                    CreatureModifierId.SmallBody,
                    CreatureModifierId.TribalNature,
                }
            };

            Assert.True(data1.getAvailableMods(stageFourData, 30, 0, 999).Any(m => m.id == CreatureModifierId.TribalNature2), "Tribal nature 2 is possible");

            //Select tribal nature 2
            data1.AddMod(CreatureModifier.allModifiers[CreatureModifierId.TribalNature2]);

            Assert.True(!data1.modifiers.Contains(CreatureModifierId.TribalNature), "Tribal nature 1 is removed");
            Assert.True(data1.modifiers.Contains(CreatureModifierId.TribalNature2), "Tribal nature 2 is present");

            //Make sure we can get 3 now
            Assert.True(data1.getAvailableMods(stageFourData, 30, 0, 999).Any(m => m.id == CreatureModifierId.TribalNature3), "Tribal nature 3 is possible");
        }

        [Test]
        public void NonProcreatedModsToPassOn()
        {
            var expectedMods = new List<CreatureModifierId>(){
                CreatureModifierId.LeanMetabolism,
                CreatureModifierId.QuickReflexes,
                CreatureModifierId.TribalNature2,
            };

            var stageFourData = new StageFourDataModel(){
                totalChildCount = 1,
                savedCreatureMods = expectedMods
            };

            var creatureData = new CreatureData(){
                speed = 2,
                modifiers = expectedMods
            };

            var modsToPass = creatureData.GetModsToPassOn(stageFourData, false);


            foreach(var mod in expectedMods){
                Assert.True(modsToPass.Contains(mod), $"{mod} was passed");
            }
        }

        //Tests which mods are passed on when you procreate.  Specifically around persistent mods
        [Test]
        public void ProcreatedModsToPassOn()
        {

            var stageFourData = new StageFourDataModel(){
                totalChildCount = 1,
                savedCreatureMods = new List<CreatureModifierId>(){
                    CreatureModifierId.LeanMetabolism,
                    CreatureModifierId.QuickReflexes,
                    CreatureModifierId.TribalNature2,
                },
                childrenBonusMods = new List<CreatureModifierId>(){
                    CreatureModifierId.Plume
                }
            };

            var creatureData = new CreatureData(){
                speed = 2,
                modifiers = new List<CreatureModifierId>(){
                    CreatureModifierId.Omnivore,
                    CreatureModifierId.SmallBody,
                    CreatureModifierId.LeanMetabolism,
                    CreatureModifierId.QuickReflexes,
                    CreatureModifierId.TribalNature3,
                }
            };

            var expectedMods = new List<CreatureModifierId>(){
                CreatureModifierId.LeanMetabolism,
                CreatureModifierId.QuickReflexes,
                CreatureModifierId.TribalNature3,
                CreatureModifierId.Plume
            };

            var modsToPass = creatureData.GetModsToPassOn(stageFourData, true);


            foreach(var mod in expectedMods){
                Assert.True(modsToPass.Contains(mod), $"{mod} was passed");
            }
        }

        //Tests recreating a new creature from saved data
        [Test]
        public void RestoringSavedMods()
        {

            var stageFourData = new StageFourDataModel(){
                totalChildCount = 1,
                savedCreatureMods = new List<CreatureModifierId>(){
                    CreatureModifierId.TribalNature2,
                    CreatureModifierId.LeanMetabolism,
                    CreatureModifierId.SharpEyesight,
                },
            };

            var creatureData = new CreatureData();
            creatureData.Reset();

            //Test isn't calling the actual code run here of course, but this helped find the issue!
            if(stageFourData.savedCreatureMods != null){
                foreach(var savedMod in stageFourData.savedCreatureMods){
                    creatureData.AddMod(CreatureModifier.allModifiers[savedMod], true);
                }
            }

            var expectedMods = new List<CreatureModifierId>(){
                CreatureModifierId.Omnivore,
                CreatureModifierId.SmallBody,
                CreatureModifierId.TribalNature2,
                CreatureModifierId.LeanMetabolism,
                CreatureModifierId.SharpEyesight,
            };


            foreach(var mod in expectedMods){
                Assert.True(creatureData.modifiers.Contains(mod), $"{mod} was passed");
            }
        }
    }
}
