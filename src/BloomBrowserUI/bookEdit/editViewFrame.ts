import theOneLocalizationManager from '../lib/localizationManager/localizationManager';
import 'jquery-ui/jquery-ui-1.10.3.custom.min.js'; //for dialog()

export function SayHello() { alert('Hello from editViewFrame'); }

// These functions should be available for calling by non-module code (such as C# directly)
// using the FrameExports object (see more details in BloomFrames.ts)
import {getToolboxFrameExports} from './js/BloomFrames';
export {getToolboxFrameExports};
import {getPageFrameExports} from './js/BloomFrames';
export {getPageFrameExports};
import {showAddPageDialog} from '../pageChooser/page-chooser';
export {showAddPageDialog};

//Called by c# using FrameExports.handleUndo()
export function handleUndo(): void {
    // First see if origami is active and knows about something we can undo.
    var contentWindow = getPageFrameExports();
    if(contentWindow && (<any>contentWindow).origamiCanUndo()) {
        (<any>contentWindow).origamiUndo();
    }
    // Undoing changes made by commands and dialogs in the toolbox can't be undone using
    // ckeditor, and has its own mechanism. Look next to see whether we know about any Undos there.
    var toolboxWindow = getToolboxFrameExports();
    if(toolboxWindow && (<any>toolboxWindow).canUndo()) {
        (<any>toolboxWindow).undo();
    } // elsewhere, we try to ask ckEditor to undo, else just the document
    else {
        // reviewslog: I don't think this will ever be executed given the current definition of canUndo.
        // I've tried to update it to the FrameExports world but am not confident it is right.
        var ckEditorUndo = this.ckEditorUndoCommand();
        if(ckEditorUndo === null || !ckEditorUndo.exec()) {
            //sometimes ckEditor isn't active, so it wasn't paying attention, so it can't do the undo. So ask the document to do an undo:
            (<any>contentWindow).document.execCommand('undo', false, null);
        }
    }
}

//Called by c# using FrameExports.canUndo()
export function canUndo(): string {
    // See comments on handleUndo()
    var contentWindow = getPageFrameExports();
    if (contentWindow && (<any>contentWindow).origamiCanUndo()) { return 'yes'; }
    var toolboxWindow = getToolboxFrameExports();
    if (toolboxWindow && (<any>toolboxWindow).canUndo()) {
        return 'yes';
    }
    /* I couldn't find a way to ask ckeditor if it is ready to do an undo.
      The "canUndo()" is misleading; what it appears to mean is, can this command (undo) be undone?*/

    /*  var ckEditorUndo = this.ckEditorUndoCommand();
        if (ckEditorUndo === null) return 'fail';
        return ckEditorUndo.canUndo() ? 'yes' : 'no';
    */

    return "fail"; //go ask the browser
}

//noinspection JSUnusedGlobalSymbols
// method called from EditingModel.cs
// for 'templatesJSON', see property EditingModel.GetJsonTemplatePageObject
