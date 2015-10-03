// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.

open System;
open System.Numerics;
open System.IO;
open Microsoft.FSharp.Data.TypeProviders;
open FSharp.Collections.ParallelSeq;

open ilf.pgn
open ilf.pgn.Data

type Result3 = {
    Name: string;
    Wins: int }

type PersonalResult(name: string, wins: int) =
    member x.Name = name
    member x.Wins = wins

[<EntryPoint>]
let main argv =     
    
    // Задача: обрабатываем данные шахматных чемпионатов
    // 1. Выводим все игры по годам
    // 2. Выводим дату игры, место и результат
    // 3. Выводим количество побед для каждого игрока    


    let chessDataDir = @"d:\.git\FSharp-Experiments\FSharpExperiments\ChessPgn\WorldChampionships\";
    let reader = new PgnReader()

    // запрос данных об играх
    let games = (Directory.EnumerateFiles(chessDataDir, "*.pgn")
        |> PSeq.map(fun x -> reader.ReadFromFile(x))
        |> Seq.collect(fun x -> x.Games))

    let gameDate (g: Game) =         
        String.Format("{0:D4}.{1:D2}.{2:D2}", 
            (if g.Year.HasValue then g.Year.Value else 0), 
            (if g.Month.HasValue then g.Month.Value else 0),
            (if g.Day.HasValue then g.Day.Value else 0))

    // 1. выводим игры по годам
    let eventProperty = [ (fun (x:Game) -> x.Event) ]
    games
        |> Seq.groupBy(fun x -> if x.Year.HasValue then x.Year.Value else 0)
        |> Seq.sortBy(fun (year, games) -> year)
        |> Seq.iter(fun (year, games) -> (
                printfn "%d" year
                games
                    |> Seq.groupBy(fun x -> x.Event)
                    |> Seq.iter(fun (x,v) -> printfn "  %s" x)
                )                
           )

    // 2. вывод даты игры, места и результата
    Console.Clear();
    games
        |> Seq.groupBy(fun x -> x.Result)
        |> Seq.iter(fun (result, games) -> (
            games
                |> Seq.iter(fun g -> (
                    let date = gameDate g
                    let str = String.Format("{0} - {1}. Result: {2}", date, g.Event, g.Result)
                    printfn "%s" str
                ))            
        ))
    
    // 3. вывод количества побед для каждого игрока по годам

    Console.Clear()    
    let f s = String.IsNullOrWhiteSpace s;
    
    games
        // фильтруем игры которые закончились победой одного из игроков
        |> PSeq.filter(fun x -> x.Result = GameResult.White || x.Result = GameResult.Black)
        // группируем по результату
        |> PSeq.groupBy(fun x -> x.Result)
        // собираем инфу
        |> PSeq.collect(fun (r, g) -> (            
            let filter = fun (x: Game) -> match r with
                | GameResult.White -> x.WhitePlayer
                | GameResult.Black -> x.BlackPlayer
            
            g
            |> Seq.groupBy(filter)
            |> Seq.map(fun (name, ggs) -> new PersonalResult (name, Seq.length ggs) )            
        ))
        |> PSeq.filter(fun x -> not (f x.Name))
        |> PSeq.sortBy(fun x -> x.Name)
        |> PSeq.iter(fun x -> printfn "%s : Wins %d" x.Name x.Wins)
       
    0 // return an integer exit code
