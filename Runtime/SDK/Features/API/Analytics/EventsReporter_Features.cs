using System.Collections.Generic;
using PrimeGames.SDK.SourceGenerator;
using PrimeGames.SDK.Common;
using UnityEngine.UIElements;

namespace PrimeGames.SDK.Features {

    public class EventsReporter_Features : FeaturesContainer {

        public new class UxmlFactory : UxmlFactory<EventsReporter_Features> { }

        public EventsReporter_Features() {
            SetInfo("Events Reporter", nameof(IAnalytics), nameof(EventsReporterProvider));

            CreateButton($"{nameof(IEventsReporter.Report)} event", () => {
                PrimeSDK.Analytics.Report("customEvent");
            });
            CreateButton($"{nameof(IEventsReporter.Report)} event value", () => {
                PrimeSDK.Analytics.Report("customEvent", "customValue");
            });
            CreateButton($"{nameof(IEventsReporter.Report)} event parameters", () => {
                PrimeSDK.Analytics.Report("customEvent", new Dictionary<string, object> {
                    { "customKey1", "customValue1" },
                    { "customKey2", "customValue2" },
                    { "customKey3", "customValue3" },
                    { "customKey4", "customValue4" }
                });
            });
        }

    }

}