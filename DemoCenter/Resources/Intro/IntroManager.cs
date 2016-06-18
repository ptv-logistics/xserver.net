using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Ptv.XServer.Demo.UseCases
{
    public class IntroManager
    {
        private readonly IntroControl[] introControls;
        private readonly string[] handlerIds;
        private const int index = 0;
        private readonly Grid location;
        public Action<string, bool> HandlerCallback;

        public IntroManager(IEnumerable<Intro> intros, Grid location)
        {
            var ctrls = new List<IntroControl>();
            var cnxts = new List<string>();

            foreach (var intro in intros)
            {
                var ctrl = new IntroControl
                {
                    Width = 500,
                    Height = 330,
                    Title = intro.Title,
                    Text = intro.Text,
                    BackButton = true,
                    NextButtonText = "Next"
                };

                switch (intro.Type)
                {
                    case Intro.PageType.First: ctrl.BackButton = false; break;
                    case Intro.PageType.Last: ctrl.NextButtonText = "Start"; break;
                    case Intro.PageType.Single: ctrl.BackButton = false; ctrl.NextButtonText = "Start"; break;
                }

                ctrls.Add(ctrl);
                cnxts.Add((String.IsNullOrEmpty(intro.HandlerId)) ? "" : intro.HandlerId);
            }

            introControls = ctrls.ToArray();
            handlerIds = cnxts.ToArray();
            
            this.location = location;
        }

        public void StartIntro()
        {
            ShowIntro(index);
        }

        private void ShowIntro(int index)
        {
            if (index < 0)
                return;

            location.Children.Add(introControls[index]);

            if (HandlerCallback != null && handlerIds.Length > index)
                HandlerCallback(handlerIds[index], true);

            introControls[index].Forwarded = () =>
            {
                if (HandlerCallback != null && handlerIds.Length > index)
                    HandlerCallback(handlerIds[index], false);

                if (introControls.Length > index)
                    location.Children.Remove(introControls[index]);

                index++;

                if (index < introControls.Length)
                    ShowIntro(index);
                else
                    HandlerCallback("_SkippedIntro", false);
            };

            introControls[index].Backwarded = () =>
            {
                if (HandlerCallback != null && handlerIds.Length > index)
                    HandlerCallback(handlerIds[index], false);

                if(introControls.Length > index)
                    location.Children.Remove(introControls[index]);

                index--;
                ShowIntro(index);
            };

            introControls[index].Skipped = yes =>
            {
                if (HandlerCallback != null)
                    HandlerCallback(handlerIds[index], false);

                location.Children.Remove(introControls[index]);
                HandlerCallback("_SkippedIntro", !yes);
            };
        }
    }

    public class Intro
    {
        public PageType Type { get; private set; }
        public string Title { get; private set; }
        public string Text { get; private set; }
        public string HandlerId { get; private set; }

        public Intro(PageType type, string handlerId, string title, string text)
        {
            Type = type;
            Title = title.ToUpper();
            Text = text;
            HandlerId = handlerId;
        }

        public enum PageType
        {
            First,
            Normal,
            Last,
            Single
        }
    }
}
