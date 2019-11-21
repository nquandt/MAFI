var scriptAddButton = document.createElement('script');
scriptAddButton.type = 'text/javascript';
scriptAddButton.text = `function callAddToAssembly(pNumber){
    mainWindowOBJ.addToAssembly(pNumber.parentElement.id);
}
function callOpenAsPart(pNumber){
    mainWindowOBJ.openAsPart(pNumber.parentElement.id);
}
function callPreLoadStep(p){
    console.log("hello");
    mainWindowOBJ.preLoadStep(p);
}
var mutationObserver = new MutationObserver(function (mutations)
{
    mutations.forEach(function (mutation)
    {       
        var nodeIDZ = null;
        mutation.addedNodes.forEach(function (nodeX)
        {            
            if (nodeX.hasChildNodes())
            {
                nodeX.childNodes.forEach(function (nodeY)
                {
                    try {
                        if (nodeY.id.includes("InLnOrd_ItmBxRw_1_"))
                        { nodeIDZ = nodeY; }
                    } catch(e) { }
                });
                try {
                    if (nodeX.id.includes("InLnOrd_ItmBxRw_1_"))
                    { nodeIDZ = nodeX; }
                } catch(e) { }
            }            
        }); 
        if (nodeIDZ != null) {
            var partNumber = "partNumber".concat(nodeIDZ.id.substr(18));           
            callPreLoadStep(partNumber);
            var nodeDiv = document.createElement("div");
            nodeDiv.id = partNumber;
            var nodeSave = nodeIDZ.getElementsByClassName("InLnOrdWebPart_TransInfo")[0]
            var nodeParent = nodeSave.parentElement;
            nodeParent.replaceChild(nodeDiv, nodeSave);
            var node = document.getElementById(partNumber);
            var newAssembleButton = document.createElement("button");
            newAssembleButton.className = "button-add-to-order-inline add-to-order customButton";
            newAssembleButton.style = "font-size:11px;";
            var newSpan = document.createElement("span");
            newSpan.className = "button-reset--IE";
            newSpan.innerText = "add to assembly";
            newAssembleButton.appendChild(newSpan);
            newAssembleButton.onclick = function () { callAddToAssembly(this); };
            var newPartButton = document.createElement("button");
            newPartButton.className = "button-add-to-order-inline add-to-order customButton";
            newPartButton.style = "font-size:11px;";
            var newPSpan = document.createElement("span");
            newPSpan.className = "button-reset--IE";
            newPSpan.innerText = "open as part";
            newPartButton.appendChild(newPSpan);
            newPartButton.onclick = function () { callOpenAsPart(this); };
            node.appendChild(newPartButton);
            node.appendChild(newAssembleButton);
        }
    });
});
mutationObserver.observe(document.documentElement,
    {
        characterData: true,
        childList: true,
        subtree: true,
        characterDataOldValue: true
    });`;
document.getElementsByTagName('head')[0].appendChild(scriptAddButton);