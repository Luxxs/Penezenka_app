using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Penezenka_App.Model;
using Penezenka_App.ViewModel;

namespace Penezenka_App.SampleData
{
    class RecordsModel
    {
        public ObservableCollection<Record> Records
        {
            get
            {
                return new ObservableCollection<Record>
                {
                    new Record
                    {
                        ID = 1,
                        Date = new DateTime(2015, 12, 1),
                        Title = "položka 1",
                        Amount = -500,
                        Notes = "sdfd fasdf asd s.",
                        Account = new Account {ID = 1, Title = "spořka", Notes = "fsdfsd"},
                        Tags = new List<Tag>
                        {
                            new Tag(1, "Jídlo", 0xFF008A00, "fddsfadsfads"),
                            new Tag(1, "Jídlo", 0xFF008A00, "fddsfadsfads"),
                            new Tag(1, "Jídlo", 0xFF008A00, "fddsfadsfads"),
                            new Tag(1, "Jídlo", 0xFF008A00, "fddsfadsfads")
                        },
                        RecurrenceChain = new RecurrenceChain{ID = 1, Type = "W", Value = 6, Disabled = false},
                        Automatically = true
                    },
                    new Record
                    {
                        ID = 2,
                        Date = new DateTime(2015, 12, 30),
                        Title = "ubytovací stipendium",
                        Amount = 600,
                        Notes = "kkkkkkkkkkkkkk",
                        Account = new Account {ID = 2, Title = "hotovost", Notes = "fsdfsd"},
                        Tags = new List<Tag>{new Tag(1, "Jídlo", 0xFF008A00, "fddsfadsfads")},
                        RecurrenceChain = new RecurrenceChain{ID = 0, Type = null},
                        Automatically = false
                    }
                };
            }
        }

        public ObservableCollection<RecordsViewModel.RecordsTagsChartMap> IncomePerTagChartMap
        {
            get
            {
                return new ObservableCollection<RecordsViewModel.RecordsTagsChartMap>
                {
                    new RecordsViewModel.RecordsTagsChartMap
                    {
                        Title = "jídlo",
                        Amount = 500
                    },
                    new RecordsViewModel.RecordsTagsChartMap
                    {
                        Title = "bydlení",
                        Amount = 2500
                    }
                };
            }
        }

        public ObservableCollection<Tag> Tags
        {
            get
            {
                return new ObservableCollection<Tag>
                {
                    new Tag(1, "Jídlo", 0xFF008A00, "fddsfadsfads"),
                    new Tag(1, "Bydlení", 0xFFFF8C00, "kkhj")
                };
            }
        }
    }
}
