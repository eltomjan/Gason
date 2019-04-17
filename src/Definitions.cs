﻿using System;

namespace Gason
{
    public enum JsonTag
    {
        JSON_NUMBER = 0,
        JSON_NUMBER_STR,
        JSON_STRING,
        JSON_ARRAY,
        JSON_OBJECT,
        JSON_TRUE,
        JSON_FALSE,
        JSON_NULL = 0xF
    }
    public enum JsonErrno
    {
        OK,
        BAD_NUMBER,
        BAD_STRING,
        BAD_IDENTIFIER,
        STACK_OVERFLOW,
        STACK_UNDERFLOW,
        MISMATCH_BRACKET,
        UNEXPECTED_CHARACTER,
        UNQUOTED_KEY,
        BREAKING_BAD,
        ALLOCATION_FAILURE
    }
    public class SearchTables
    { // 8B - double or U64
        public static Byte[] valTypes = new byte[256] { // used #: 1-8, 10-15
            0,0,0,0, 0,0,0,0, 0,1,1,0, 0,1,0,0, // 0-15
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,1, // 16-31
          //  ! " #  $ % & '  ( ) * +   , - . /
            0,0,3,0, 0,0,0,0, 0,0,0,0, 15,2,0,0, // 32-47
          //0 1 2 3  4 5 6 7  8 9  : ;  < = > ?
            4,4,4,4, 4,4,4,4, 4,4,14,0, 0,0,0,0, // 48-63
          //@ A B C  D E F G  H I J K  L M N O
            0,0,0,0, 0,5,0,0, 0,0,0,0, 0,0,0,0, // 64-79
          //P Q R S  T U V W  X Y Z  [  \  ] ^ _
            0,0,0,0, 0,0,0,0, 0,0,0,11, 0,12,0,0, // 80-95
          //` a b c  d e f g  h i j k  l m n o
            0,0,0,0, 0,5,6,0, 0,0,0,0, 0,0,8,0, // 96-111 // 6 => f-alse 8 => n-ull
          //p q r s  t u v w  x y z  {  |  } ~ ⌂
            0,0,0,0, 7,0,0,0, 0,0,0,10, 0,13,0,0, // 112-127 // 7 => t-rue
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, // 128-143
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, // 144-159
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, // 160-175
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, // 176-191
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, // 192-207
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, // 208-223
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, // 224-239
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0 // 240-255
        };
        public static Byte[] specialTypes = new byte[256] {
          //0 1 2 3  4 5 6 7  8 9 A B  C D E F
            2,0,0,0, 0,0,0,0, 0,1,1,0, 0,1,0,0, // 0-15  1 => \t\n\r spaces / 2 => isdelim
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, // 16-31
          //  ! " #  $ % & '  ( ) * +  , - . /
            1,0,0,0, 0,0,0,0, 0,0,0,0, 2,0,0,0, // 32-47 space-> 1, -> 2
          //0 1 2 3  4 5 6 7  8 9 : ;  < = > ?
            0,0,0,0, 0,0,0,0, 0,0,2,0, 0,0,0,0, // 48-63 : -> 
          //@ A B C  D E F G  H I J K  L M N O
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, // 64-79
          //P Q R S  T U V W  X Y Z [  \ ] ^ _
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,2,0,0, // 80-95
          //` a b c  d e f g  h i j k  l m n o
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, // 96-111
          //p q r s  t u v w  x y z {  | } ~ ⌂
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,2,0,0, // 112-127 } -> 2
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, // 128-143
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, // 144-159
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, // 160-175
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, // 176-191
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, // 192-207
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, // 208-223
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0, // 224-239
            0,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0 // 240-255
        };
    }
}
