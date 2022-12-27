using System;

namespace AtaraxiaAI.Data.Domains
{
    [Serializable]
    public class GrammarChoice
    {
        public string Word { get; set; }
        public Classifications? Classification { get; set; }
        public NounTypes? NounType { get; set; }

        // shorturl.at/opW37
        public enum Classifications
        {
            Adjective,
            Adverb,
            Conjunction,
            Interjection,
            Noun,
            Preposition,
            Pronoun,
            Verb
        }

        public enum NounTypes
        {
            Person,
            Place,
            Thing
        }
    }
}
