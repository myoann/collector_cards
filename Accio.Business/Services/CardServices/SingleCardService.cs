﻿using Accio.Business.Models.CardModels;
using Accio.Business.Models.LanguageModels;
using Accio.Business.Services.CardSearchHistoryServices;
using Accio.Business.Services.LanguageServices;
using Accio.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Accio.Business.Services.CardServices
{
    public class SingleCardService
    {
        private AccioContext _context { get; set; }
        private TypeService _cardTypeService { get; set; }
        private LanguageService _languageService { get; set; }
        private CardSearchHistoryService _cardSearchHistoryService { get; set; }
        private CardSubTypeService _cardSubTypeService { get; set; }
        private CardImageService _cardImageService { get; set; }
        private CardRulingRestrictionService _cardRulingRestrictionService { get; set; }

        public SingleCardService(AccioContext context, TypeService cardTypeService, LanguageService languageService,
                                 CardSearchHistoryService cardSearchHistoryService, CardSubTypeService cardSubTypeService,
                                 CardImageService cardImageService, CardRulingRestrictionService cardRulingRestrictionService)
        {
            _context = context;
            _cardTypeService = cardTypeService;
            _languageService = languageService;
            _cardSearchHistoryService = cardSearchHistoryService;
            _cardSubTypeService = cardSubTypeService;
            _cardImageService = cardImageService;
            _cardRulingRestrictionService = cardRulingRestrictionService;
        }

        public CardModel GetCard(SingleCardParameters singleCardParameters)
        {
            var param = singleCardParameters;
            var utcNow = DateTime.UtcNow;

            if (param.LanguageId == null || param.LanguageId == Guid.Empty)
            {
                var englishLanguageId = _languageService.GetLanguageId(TypeOfLanguage.English);
                param.LanguageId = englishLanguageId;
            }

            var cards = (from card in _context.Cards
                         join cardDetail in _context.CardDetails on card.CardId equals cardDetail.CardId
                         join language in _context.Languages on cardDetail.LanguageId equals language.LanguageId
                         join cardSet in _context.Sets on card.CardSetId equals cardSet.SetId
                         join cardRarity in _context.Rarities on card.CardRarityId equals cardRarity.RarityId
                         join cardType in _context.CardTypes on card.CardTypeId equals cardType.CardTypeId
                         join lessonType in _context.LessonTypes on card.LessonTypeId equals lessonType.LessonTypeId into lessonTypeDefault
                         from lessonType in lessonTypeDefault.DefaultIfEmpty()
                         join provides in _context.CardProvidesLessons on card.CardId equals provides.CardId into cardsProvidesLesson
                         from provides in cardsProvidesLesson.DefaultIfEmpty()
                         join plesson in _context.LessonTypes on provides.LessonId equals plesson.LessonTypeId into providesLesson
                         from plesson in providesLesson.DefaultIfEmpty()
                         where !card.Deleted && !cardSet.Deleted && !cardRarity.Deleted && !cardType.Deleted &&
                               language.LanguageId == param.LanguageId && cardSet.ShortName == param.SetShortName &&
                               card.CardNumber == param.CardNumber
                         select new
                         {
                             card,
                             cardDetail,
                             cardSet,
                             cardRarity,
                             cardType,
                             language,
                             lessonType,
                             plesson,
                             provides
                         });

            var cardModel = cards.Select(x => CardService.GetCardModel(x.card, x.cardSet, x.cardRarity, x.cardType, x.cardDetail,
                                                           x.language, x.lessonType, x.plesson, x.provides)).SingleOrDefault();
            if (cardModel != null)
            {
                cardModel.SubTypes = _cardSubTypeService.GetCardSubTypes(cardModel.CardId);
                cardModel = GetCardsWithImages(new List<CardModel>() { cardModel })[0];
                cardModel.MetaDescription = GetMetaDescription(cardModel);
                cardModel.RulingRestrictions = _cardRulingRestrictionService.GetCardRulingRestrictionsByCardId(cardModel.CardId).OrderBy(x => x.Format).ToList();

                _cardSearchHistoryService.PersistCardSearchHistory(param, utcNow, utcNow);
            }

            return cardModel;
        }
        /// <summary>
        /// Returns the card route parameters from the card ID, to help ensure backwards compatibility with the old way of showing the card page
        /// </summary>
        public SingleCardRoute GetSingleCardRoute(Guid cardId)
        {
            var val = (from card in _context.Cards
                       join cardDetail in _context.CardDetails on card.CardId equals cardDetail.CardId
                       join cardSet in _context.Sets on card.CardSetId equals cardSet.SetId
                       where !card.Deleted && !cardSet.Deleted && card.CardId == cardId
                       select new SingleCardRoute()
                       {
                           SetShortName = cardSet.ShortName,
                           CardNumber = card.CardNumber,
                           CardName = cardDetail.Name.Replace(" ", "-"),
                       }).SingleOrDefault();

            return val;
        }

        private List<CardModel> GetCardsWithImages(List<CardModel> cards)
        {
            var cardImages = _cardImageService.GetCardImages(cards.Select(x => x.CardId).ToList());
            foreach (var card in cards)
            {
                var images = cardImages.Where(x => x.CardId == card.CardId && x.Image.Language.LanguageId == card.Detail.Language.LanguageId).ToList();
                if (images?.Count > 0)
                {
                    card.Images = images.Select(x => x.Image).ToList();
                }
            }

            return cards;
        }

        private string GetMetaDescription(CardModel card)
        {
            var desc = $"{card.CardType.Name}";

            if (card.SubTypes.Count > 0)
            {
                desc += $" - {string.Join(" - ", card.SubTypes.Select(x => x.SubType.Name).ToList())} • ";
            }

            if (!desc.EndsWith(" • "))
            {
                desc += " • ";
            }

            var toc = _cardTypeService.GetTypeOfCard(card.CardType.CardTypeId);
            switch (toc)
            {

                case Models.TypeModels.TypeOfCard.Adventure:
                case Models.TypeModels.TypeOfCard.Match:
                    desc += $"{card.Detail.Effect} {card.Detail.ToSolve} {card.Detail.Reward} • ";

                    break;
                case Models.TypeModels.TypeOfCard.Character:
                case Models.TypeModels.TypeOfCard.Creature:
                case Models.TypeModels.TypeOfCard.Item:
                case Models.TypeModels.TypeOfCard.Lesson:
                case Models.TypeModels.TypeOfCard.Location:
                case Models.TypeModels.TypeOfCard.Spell:
                    desc += $"{card.Detail.Text} • ";

                    break;
            }

            desc += $"#{card.CardNumber} • Illustrated by {card.Detail.Illustrator} • Harry Potter TCG";

            return desc;
        }
    }
}
