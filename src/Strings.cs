using System;

namespace Gason
{
    public class Strings
    {
        public static String JSONnetComplete { get { return _JSONnetComplete.Replace('\'', '"'); }  }
        public static String JSONnetPart1 { get { return _JSONnetPart1.Replace('\'', '"'); } }
        public static String JSONnetPart2 { get { return _JSONnetPart2.Replace('\'', '"'); } }
        public static String Sort1 { get { return _Sort1.Replace('\'', '"'); } }
        public static String Sort2 { get { return _Sort2.Replace('\'', '"'); } }
        public static String Twitter1 { get { return _Twitter1.Replace('\'', '"'); } }
        public static String Twitter2 { get { return _Twitter2.Replace('\'', '"'); } }
        public const String _JSONnetComplete = @"{
  'id': '000',
  'id': '00',
  'id': '0',
  'id': '0001',
  'type': 'donut',
  'name': 'Cake',
  'ppu': 0.55,
  'batters': [
    {
      'id': '1004',
      'type': 'Bad Food'
    },
    {
      'id': '1001',
      'type': 'Regular'
    },
    {
      'id': '1003',
      'type': 'Blueberry'
    },
    {
      'id': '1002',
      'type': 'Chocolate'
    }
  ]
}", _JSONnetPart1 = @"{
  'batters': [
    {
      'id': '1002',
      'type': 'Chocolate'
    },
    {
      'id': '1003',
      'type': 'Blueberry'
    },
    {
      'id': '1004',
      'type': 'Bad Food'
    }
  ],
  'id': '0001',
  'type': 'donut',
  'name': 'Cake',
  'ppu': 0.55
}", _JSONnetPart2 = @"{
  'id': '0001',
  'type': 'donut',
  'name': 'Cake',
  'ppu': 0.55
  'batters': [
    {
      'id': '1001',
      'type': 'Regular'
    },
    {
      'id': '1003',
      'type': 'Blueberry'
    },
    {
      'id': '1004',
      'type': 'Bad Food'
    }
  ]
}", _Sort1 = @"{
  'id': '0',
  'id': '00',
  'id': '000',
  'id': '0001',
  'name': 'Cake',
  'ppu': 0.55,
  'type': 'donut',
  'batters': [
    {
      'id': '1003',
      'type': 'Blueberry'
    },
    {
      'id': '1002',
      'type': 'Chocolate'
    },
    {
      'id': '1004',
      'type': 'Bad Food'
    },
    {
      'id': '1001',
      'type': 'Regular'
    }
  ]
}
", _Sort2 = @"{
  'batters': [
    {
      'id': '1001',
      'type': 'Regular'
    },
    {
      'id': '1002',
      'type': 'Chocolate'
    },
    {
      'id': '1003',
      'type': 'Blueberry'
    },
    {
      'id': '1004',
      'type': 'Bad Food'
    }
  ],
  'id': '0',
  'id': '00',
  'id': '000',
  'id': '0001',
  'name': 'Cake',
  'ppu': 0.55,
  'type': 'donut'
}
", _Twitter1 = @"{
  'statuses': [
    {
      'entities': {
        'hashtags': [
          {
            'Sun'
          }
        ]
      }
    }
  ]
}
", _Twitter2 = @"{
  'statuses': [
    {
      'entities': {
        'user_mentions': [
          {
            'indices': [
              '2',
              '3'
            ]
          }
        ]
      }
    },
    {
      'entities': {
        'hashtags': [
          {
            'Aug'
          }
        ]
      }
    }
  ]
}
";
    }
}