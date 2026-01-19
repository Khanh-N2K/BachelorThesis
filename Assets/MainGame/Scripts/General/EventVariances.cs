using System;
using UnityEngine;

public static class EventVariances
{
    public static Action onAttackerCountUpdated;

    public struct CardSystem
    {
        public static Action<Card> onCardClicked;
        public static Action<Card> onCardReleased;

        public static Action<Vector2> onStartHoveredOnMap;  // screen pos
        public static Action onEndHoveredOnMap;

        public static Action<Card> onStartHoveredOnSellArea;
        public static Action onEndHoveredOnSellArea;
    }

    public struct MerchantUI
    {
        public static Action onUIInteracted;

        public static Action onShowMainPanel;
        public static Action onHideMainPanel;

        public static Action<CardConfig, Vector2> onBuyCard;    // Config, spawn card screen pos
    }
}
