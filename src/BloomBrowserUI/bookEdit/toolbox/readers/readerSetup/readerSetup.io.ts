/// <reference path="../ReaderSettings.ts" />
/// <reference path="readerSetup.ui.ts" />
import {
    enableSampleWords,
    displayLetters,
    selectLetters,
    selectLevel,
    forcePlainTextPaste,
    selectStage,
    setLevelValue
} from "./readerSetup.ui";
import { ReaderStage, ReaderLevel, ReaderSettings } from "../ReaderSettings";
import "../../../../lib/jquery.onSafe.js";
import * as _ from "underscore";

interface ILevelSetting {
    // in the right hand panel, the span for editing the value has this ID prefixed with "max-",
    // and the check box for enabling it has this ID prefixed with "use-"
    // In the left pane, this is the class for the span containing the value
    cellClass: string;
    // functions to get and set the value in the level object
    getter: (level: ReaderLevel) => number;
    setter: (level: ReaderLevel, v: number) => number;
    // Value in parens in same cell; a subcell setting may not have further subcells.
    subcell?: ILevelSetting;
}

// Data to help configure the things that differ for each level setting.
export const levelSettings: ILevelSetting[] = [
    {
        cellClass: "glyphs-per-word",
        getter: level => level.maxGlyphsPerWord,
        setter: (level: ReaderLevel, v: number) => (level.maxGlyphsPerWord = v),
        subcell: {
            cellClass: "average-glyphs-per-word",
            getter: level => level.maxAverageGlyphsPerWord,
            setter: (level: ReaderLevel, v: number) =>
                (level.maxAverageGlyphsPerWord = v)
        }
    },
    {
        cellClass: "words-per-sentence",
        getter: level => level.maxWordsPerSentence,
        setter: (level: ReaderLevel, v: number) =>
            (level.maxWordsPerSentence = v),
        subcell: {
            cellClass: "average-words-per-sentence",
            getter: level => level.maxAverageWordsPerSentence,
            setter: (level: ReaderLevel, v: number) =>
                (level.maxAverageWordsPerSentence = v)
        }
    },
    {
        cellClass: "words-per-page",
        getter: level => level.maxWordsPerPage,
        setter: (level: ReaderLevel, v: number) => (level.maxWordsPerPage = v),
        subcell: {
            cellClass: "average-words-per-page",
            getter: level => level.maxAverageWordsPerPage,
            setter: (level: ReaderLevel, v: number) =>
                (level.maxAverageWordsPerPage = v)
        }
    },
    {
        cellClass: "words-per-book",
        getter: level => level.maxWordsPerBook,
        setter: (level: ReaderLevel, v: number) => (level.maxWordsPerBook = v),
        subcell: {
            cellClass: "unique-words-per-book",
            getter: level => level.maxUniqueWordsPerBook,
            setter: (level: ReaderLevel, v: number) =>
                (level.maxUniqueWordsPerBook = v)
        }
    },
    {
        cellClass: "sentences-per-page",
        getter: level => level.maxSentencesPerPage,
        setter: (level: ReaderLevel, v: number) =>
            (level.maxSentencesPerPage = v),
        subcell: {
            cellClass: "average-sentences-per-page",
            getter: level => level.maxAverageSentencesPerPage,
            setter: (level: ReaderLevel, v: number) =>
                (level.maxAverageSentencesPerPage = v)
        }
    }
];

let previousMoreWords: string;

window.addEventListener("message", process_IO_Message, false);

export interface ToolboxWindow extends Window {
    editTabBundle: any;
}
export function toolboxWindow(): ToolboxWindow | undefined {
    if (window.parent) {
        const toolboxFrameElement = window.parent.document.getElementById(
            "toolbox"
        );
        if (toolboxFrameElement) {
            const window = (<HTMLIFrameElement>toolboxFrameElement)
                .contentWindow;
            if (window) {
                return window as ToolboxWindow;
            }
        }
    }
    return undefined;
}

function process_IO_Message(event: MessageEvent): void {
    const params = event.data.split("\n");

    switch (params[0]) {
        case "OK":
            saveClicked();
            return;

        case "Data":
            loadReaderSetupData(params[1]);
            const win = toolboxWindow();
            if (win) {
                win.postMessage("SetupType", "*");
            }
            return;

        default:
    }
}
export function setPreviousMoreWords(words: string) {
    previousMoreWords = words;
}
export function getPreviousMoreWords(): string {
    return previousMoreWords;
}

// Return the standard span HTML for displaying the specified setting of the given level.
// Format as a subcell (surround with parens) if it has a value and subcell is true.
function spanForLevelSetting(
    level: ReaderLevel,
    s: ILevelSetting,
    subcell: boolean
): string {
    return spanForSettingWithText(s, setLevelValue(s.getter(level)), subcell);
}
// Return the standard span HTML for displaying the specified setting, given its
// value as content. Format as a subcell if that argument is true.
// Note: the keyUp handler attached to .level-textbox by attachEventHandlers() in
// readerSetup.ui.ts unfortunately also has detailed knowledge of how setting
// spans and subcells are formatted.
export function spanForSettingWithText(
    s: ILevelSetting,
    content: string,
    subcell: boolean
): string {
    // In subcells we don't display the "-" if the value is not set.
    const adjustedContent = subcell && content === "-" ? "" : content;
    // We want the span even if it is empty. It marks where content will
    // be inserted if the user types it on the right.
    const span = `<span class="${s.cellClass}">${adjustedContent}</span>`;
    if (subcell && adjustedContent) {
        return " (" + span + ")";
    }
    return span;
}

/**
 * Initializes the dialog with the current settings.
 * @param {String} jsonData The contents of the settings file
 */
function loadReaderSetupData(jsonData: string): void {
    if (!jsonData || jsonData === '""') return;

    // validate data
    const data = JSON.parse(jsonData);
    if (!data.letters) data.letters = "";
    if (!data.moreWords) data.moreWords = "";
    if (!data.stages) data.stages = [];
    if (!data.levels) data.levels = [];
    if (data.stages.length === 0) data.stages.push(new ReaderStage("1"));
    if (data.levels.length === 0) data.levels.push(new ReaderLevel("1"));
    if (!data.useAllowedWords) data.useAllowedWords = 0;
    if (!data.sentencePunct) data.sentencePunct = "";

    // language tab
    (<HTMLInputElement>document.getElementById("dls_letters")).value =
        data.letters;
    (<HTMLInputElement>document.getElementById("dls_sentence_punct")).value =
        data.sentencePunct;
    setPreviousMoreWords(data.moreWords.replace(/ /g, "\n"));
    (<HTMLInputElement>(
        document.getElementById("dls_more_words")
    )).value = previousMoreWords;
    $(
        'input[name="words-or-letters"][value="' + data.useAllowedWords + '"]'
    ).prop("checked", true);
    enableSampleWords();

    // stages tab
    displayLetters();
    const stages = data.stages;
    const tbody = $("#stages-table").find("tbody");
    tbody.html("");

    for (let i = 0; i < stages.length; i++) {
        if (!stages[i].letters) stages[i].letters = "";
        if (!stages[i].sightWords) stages[i].sightWords = "";
        if (!stages[i].allowedWordsFile) stages[i].allowedWordsFile = "";
        tbody.append(
            '<tr class="linked"><td>' +
                (i + 1) +
                '</td><td class="book-font lang1InATool">' +
                stages[i].letters +
                '</td><td class="book-font lang1InATool">' +
                stages[i].sightWords +
                '</td><td class="book-font lang1InATool">' +
                stages[i].allowedWordsFile +
                "</td></tr>"
        );
    }

    // click event for stage rows
    tbody.find("tr").onSafe("click", function() {
        selectStage(this);
        displayLetters();
        selectLetters(this);
    });

    // levels tab
    const levels = data.levels;
    const tbodyLevels = $("#levels-table").find("tbody");
    tbodyLevels.html("");
    for (let j = 0; j < levels.length; j++) {
        const level = levels[j];
        tbodyLevels.append(
            '<tr class="linked"><td>' +
                (j + 1) +
                "</td>" +
                levelSettings
                    .map(s => {
                        const subcell = s.subcell
                            ? `${spanForLevelSetting(level, s.subcell, true)}`
                            : "";
                        return `<td>${spanForLevelSetting(
                            level,
                            s,
                            false
                        )}${subcell}</td>`;
                    })
                    .join("") +
                '<td style="display: none">' +
                level.thingsToRemember.join("\n") +
                "</td></tr>"
        );
    }

    // click event for level rows
    tbodyLevels.find("tr").onSafe("click", function() {
        selectLevel(this);
    });

    // Prevent pasting HTML markup (or anything else but plain text) anywhere.
    // Note that anything that creates new contenteditable elements (e.g., when selecting a level
    // or stage) needs to call this on the appropriate parent.
    forcePlainTextPaste(document);
}

function saveClicked(): void {
    beginSaveChangedSettings(); // don't wait for full refresh
    const win = toolboxWindow();
    if (win) {
        win.editTabBundle.closeSetupDialog();
    }
}

/**
 * Pass the settings to the server to be saved
 */
export function beginSaveChangedSettings(): JQueryPromise<void> {
    const settings = getChangedSettings();
    // Be careful here! After we return this promise, this dialog (and its iframe) may close and the iframe code
    // (including this method here) gets unloaded. So it's important that the block of code that saves the settings and updates things
    // is part of the main toolbox code, NOT part of this method. When it was in this method, bizarre things
    // happened, such as calling the axios post method to save the settings...but C# never received them,
    // and the 'then' clause never got invoked.
    const win = toolboxWindow();
    if (win) {
        return win.editTabBundle.beginSaveChangedSettings(
            settings,
            previousMoreWords
        );
    }
    // paranoia section
    const result = $.Deferred<void>();
    return result.resolve();
}

function getChangedSettings(): ReaderSettings {
    const settings: ReaderSettings = new ReaderSettings();
    settings.letters = cleanSpaceDelimitedList(
        (<HTMLInputElement>document.getElementById("dls_letters")).value
    );
    settings.sentencePunct = (<HTMLInputElement>(
        document.getElementById("dls_sentence_punct")
    )).value.trim();

    // remove duplicates from the more words list
    let moreWords: string[] = _.uniq(
        (<HTMLInputElement>(
            document.getElementById("dls_more_words")
        )).value.split("\n")
    );

    // remove empty lines from the more words list
    moreWords = _.filter(moreWords, (a: string) => {
        return a.trim() !== "";
    });
    settings.moreWords = moreWords.join(" ");

    settings.useAllowedWords = parseInt(
        $('input[name="words-or-letters"]:checked').val()
    );

    // stages
    const stages: JQuery = $("#stages-table").find("tbody tr");
    let iStage: number = 1;
    for (let iRow: number = 0; iRow < stages.length; iRow++) {
        const stage: ReaderStage = new ReaderStage(iStage.toString());
        const row: HTMLTableRowElement = <HTMLTableRowElement>stages[iRow];
        stage.letters = (<HTMLTableCellElement>row.cells[1]).innerHTML;
        stage.sightWords = cleanSpaceDelimitedList(
            (<HTMLTableCellElement>row.cells[2]).innerHTML
        );
        stage.allowedWordsFile = (<HTMLTableCellElement>row.cells[3]).innerHTML;

        // do not save stage with no data
        if (stage.letters || stage.sightWords || stage.allowedWordsFile) {
            settings.stages.push(stage);
            iStage++;
        }
    }

    // levels
    const levels: JQuery = $("#levels-table").find("tbody tr");
    for (let j: number = 0; j < levels.length; j++) {
        const level: ReaderLevel = new ReaderLevel((j + 1).toString());
        // @ts-ignore
        delete level.name; //I don't know why this has a name, but it's apparently just part of the UI that we don't want to save
        const row: HTMLTableRowElement = <HTMLTableRowElement>levels[j];
        for (let k = 0; k < levelSettings.length; k++) {
            const levelSetting = levelSettings[k];
            extractSettingValue(row, levelSetting, level);
            if (levelSetting.subcell) {
                extractSettingValue(row, levelSetting.subcell, level);
            }
        }
        level.thingsToRemember = (<HTMLTableCellElement>(
            row.cells[levelSettings.length + 1]
        )).innerHTML.split("\n");
        settings.levels.push(level);
    }
    return settings;
}

function extractSettingValue(
    row: HTMLTableRowElement,
    levelSetting: ILevelSetting,
    level: ReaderLevel
) {
    const val = row.getElementsByClassName(levelSetting.cellClass)[0];
    levelSetting.setter(level, getLevelValue(val.innerHTML));
}

function getLevelValue(innerHTML: string): number {
    innerHTML = innerHTML.trim();
    if (innerHTML === "-") return 0;
    return parseInt(innerHTML);
}

/**
 * if the user enters a comma-separated list, remove the commas before saving (this is a space-delimited list)
 * Also converts newlines to spaces.
 * @param original
 * @returns {string}
 */
export function cleanSpaceDelimitedList(original: string): string {
    let cleaned: string = original
        .replace(/,/g, " ")
        .replace(/\r/g, " ")
        .replace(/\n/g, " "); // replace commas and newlines
    cleaned = cleaned.trim().replace(/ ( )+/g, " "); // remove consecutive spaces

    return cleaned;
}
