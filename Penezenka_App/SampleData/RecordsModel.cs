using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Penezenka_App.Model;
using Penezenka_App.OtherClasses;
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
                        Date = new DateTime(2015, 3, 29),
                        Title = "položka 1",
                        Amount = -500,
                        Notes = "sdfd fasdf asd s.",
                        Account = new Account {ID = 1, Title = "spořka", Notes = "fsdfsd"},
                        Tags = new List<Tag>{new Tag(1, "Jídlo", 0xFF008A00, "fddsfadsfads")},
                        RecurrenceChain = new RecurrenceChain{ID = 1, Type = "W", Value = 6, Disabled = false},
                        Automatically = true
                    },
                    new Record
                    {
                        ID = 2,
                        Date = new DateTime(2015, 3, 30),
                        Title = "položka 2",
                        Amount = -490,
                        Notes = "kkkkkkkkkkkkkk",
                        Account = new Account {ID = 2, Title = "hotovost", Notes = "fsdfsd"},
                        Tags = new List<Tag>{new Tag(1, "Jídlo", 0xFF008A00, "fddsfadsfads")},
                        RecurrenceChain = new RecurrenceChain{ID = 0, Type = null},
                        Automatically = true
                    }
                };
            }
        }

        public ObservableCollection<RecordsTagsChartMap> RecordsPerTagChartMap
        {
            get
            {
                return new ObservableCollection<RecordsTagsChartMap>
                {
                    new RecordsTagsChartMap
                    {
                        Title = "jídlo",
                        Amount = 500
                    },
                    new RecordsTagsChartMap
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

        public ObservableCollection<Record> PendingRecurrentRecords
        {
            get
            {
                return new ObservableCollection<Record>
                {
                    new Record
                    {
                        ID = 1,
                        Date = new DateTime(2015, 4, 11),
                        Title = "elektrika",
                        Amount = -2500,
                        Notes = "sdfd fasdf asd s.",
                        Account = new Account {ID = 1, Title = "spořka", Notes = "fsdfsd"},
                        Tags = new List<Tag>{new Tag(1, "Jídlo", 0xFF008A00, "fddsfadsfads")},
                        RecurrenceChain = new RecurrenceChain{ID = 1, Type = "M", Value = 11, Disabled = false},
                        Automatically = true
                    },
                    new Record
                    {
                        ID = 2,
                        Date = new DateTime(2015, 4, 6),
                        Title = "stipendium",
                        Amount = 600,
                        Notes = "univerzita pardubice",
                        Account = new Account {ID = 2, Title = "spořka", Notes = "fsdfsd"},
                        Tags = new List<Tag>{new Tag(1, "Jídlo", 0xFF008A00, "fddsfadsfads")},
                        RecurrenceChain = new RecurrenceChain{ID = 1, Type = "M", Value = 6, Disabled = false},
                        Automatically = true
                    },
                    new Record
                    {
                        ID = 2,
                        Date = new DateTime(2015, 3, 30),
                        Title = "nějaký týdenní",
                        Amount = 50,
                        Notes = "kkkkkkkkkkkkkk",
                        Account = new Account {ID = 2, Title = "spořka", Notes = "fsdfsd"},
                        Tags = new List<Tag>{new Tag(1, "Jídlo", 0xFF008A00, "fddsfadsfads")},
                        RecurrenceChain = new RecurrenceChain{ID = 1, Type = "W", Value = 1, Disabled = false},
                        Automatically = true
                    }
                };
            }
        }
    }
}
