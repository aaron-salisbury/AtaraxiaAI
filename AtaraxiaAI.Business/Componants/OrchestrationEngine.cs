﻿using AtaraxiaAI.Business.Skills;
using System;
using System.Linq;

namespace AtaraxiaAI.Business.Componants
{
    public class OrchestrationEngine
    {
        public const string WAKE_COMMAND = "Hey Robot";

        public enum SkillMessages
        {
            TellMeAJoke
        }

        private SpeechEngine _speechEngine;

        public OrchestrationEngine(SpeechEngine speechEngine)
        {
            _speechEngine = speechEngine;
        }

        public void Heard(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                AI.Log.Logger.Information($"Heard \"{message}\".");

                if (message.StartsWith(WAKE_COMMAND))
                {
                    int wakeIndex = message.IndexOf(WAKE_COMMAND);
                    string command = message.Remove(wakeIndex, WAKE_COMMAND.Length);
                    string cleanCommand = string.Concat(command.Where(c => !char.IsWhiteSpace(c)));

                    switch ((SkillMessages)Enum.Parse(typeof(SkillMessages), cleanCommand))
                    {
                        case SkillMessages.TellMeAJoke:
                            JokeSkill.TellMeAJoke(_speechEngine);
                            break;
                        default:
                            if (GPT3Skill.IsAvailable())
                            {
                                GPT3Skill.AnswerMe(command, _speechEngine).Wait();
                            }
                            break;
                    }
                }
            }
        }
    }
}
