using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSP;
using Experience;

namespace ContractConfigurator
{
    public class Kerbal
    {
        static Random random = new Random();
        static ConfigNode[] traitConfigs = null;

        public ProtoCrewMember _pcm;
        public ProtoCrewMember pcm
        {
            get
            {
                return _pcm ?? (HighLogic.CurrentGame == null ? null : HighLogic.CurrentGame.CrewRoster.AllKerbals().Where(pcm => pcm.name == name).FirstOrDefault());
            }
        }

        public string name;
        public ProtoCrewMember.Gender gender;
        public string experienceTrait;
        public ProtoCrewMember.KerbalType kerbalType;

        public float experience
        {
            get
            {
                return _pcm == null ? 0.0f : _pcm.experience;
            }
        }

        public int experienceLevel
        {
            get
            {
                return _pcm == null ? 1 : _pcm.experienceLevel;
            }
        }

        public ProtoCrewMember.RosterStatus rosterStatus
        {
            get
            {
                return _pcm == null ? ProtoCrewMember.RosterStatus.Dead : _pcm.rosterStatus;
            }
        }

        public Kerbal()
        {
            Initialize();
        }

        public Kerbal(string name)
        {
            Initialize(name);
        }

        public Kerbal(ProtoCrewMember.Gender gender)
        {
            Initialize();

            this.gender = gender;
            name = CrewGenerator.GetRandomName(gender, random);
        }

        public Kerbal(ProtoCrewMember.Gender gender, string name)
        {
            Initialize(name);

            this.gender = gender;
            this.name = name;
        }

        public Kerbal(ProtoCrewMember.Gender gender, string name, string experienceTrait)
        {
            Initialize(name);

            this.gender = gender;
            this.name = name;
            this.experienceTrait = experienceTrait;
        }

        public void Initialize(string name = null)
        {
            if (name != null)
            {
                this.name = name;
                _pcm = pcm;
            }

            if (_pcm != null)
            {
                SetCrew(_pcm);
            }
            else
            {
                RandomizeGender();
                this.name = name ?? CrewGenerator.GetRandomName(gender, random);
                experienceTrait = RandomExperienceTrait();
                kerbalType = ProtoCrewMember.KerbalType.Applicant;
            }
        }

        public Kerbal(ProtoCrewMember pcm)
        {
            SetCrew(pcm);
        }

        public void SetCrew(ProtoCrewMember pcm)
        {
            this._pcm = pcm;
            gender = pcm.gender;
            name = pcm.name;
            experienceTrait = pcm.experienceTrait.TypeName;
            kerbalType = pcm.type;
        }

        public string RandomExperienceTrait()
        {
            // This needs to work even when a game isn't loaded, so go to the game database instead of the classes
            if (traitConfigs == null)
            {
                traitConfigs = GameDatabase.Instance.GetConfigNodes("EXPERIENCE_TRAIT");
            }

            int r = random.Next(traitConfigs.Count());
            return traitConfigs.ElementAt(r).GetValue("name");
        }

        public void RandomizeGender()
        {
            gender = random.Next(2) == 0 ? ProtoCrewMember.Gender.Male : ProtoCrewMember.Gender.Female;
        }

        public void GenerateKerbal()
        {
            _pcm = HighLogic.CurrentGame.CrewRoster.GetNewKerbal(kerbalType);
            _pcm.gender = gender;
            _pcm.name = name;
            KerbalRoster.SetExperienceTrait(_pcm, experienceTrait);
        }

        public override string ToString()
        {
            return name;
        }
    }
}
