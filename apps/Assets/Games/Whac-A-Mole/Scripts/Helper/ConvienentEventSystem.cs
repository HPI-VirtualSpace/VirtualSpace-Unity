using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualSpace.Messaging
{
    public class ConvienentEvent
    {
        public float triggerIn;
        public Action trigger;

        public ConvienentEvent(float triggerIn, Action trigger)
        {
            this.triggerIn = triggerIn;
            this.trigger = trigger;
        }
    }

    public abstract class ConvienentEventSystem : MonoBehaviour
    {
        public bool paused = true;

        private List<ConvienentEvent> _pendingEvents = new List<ConvienentEvent>();
        protected float _pauseTime;
        protected float _executionTime;

        private void Update()
        {
            if (paused)
            {
                _pauseTime += Time.deltaTime;
                return;
            }
            AdvanceTime(Time.deltaTime);
            _executionTime += Time.deltaTime;
        }

        private void AdvanceTime(float time)
        {
            List<ConvienentEvent> eventsToFire = new List<ConvienentEvent>();
            foreach (ConvienentEvent pendingEvent in _pendingEvents)
            {
                pendingEvent.triggerIn -= time;

                if (pendingEvent.triggerIn <= 0)
                {
                    eventsToFire.Add(pendingEvent);
                }
            }

            foreach (ConvienentEvent pendingEvent in eventsToFire)
            {
                pendingEvent.trigger();
                _pendingEvents.Remove(pendingEvent);
            }
        }

        protected void AddEvent(ConvienentEvent newEvent)
        {
            _pendingEvents.Add(newEvent);
        }

        private void OnDestroy()
        {
            _pendingEvents.Clear();
        }
    }
}