function callAddToAssembly(){
    var customButtons = document.getElementsByClassName("customButton");
    for (var i = 0; i < customButtons.length; i++)
    { 
        customButtons[i].style.backgroundColor = "blue"; 
    }
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
            var nodeDiv = document.createElement("div");
            nodeDiv.id = partNumber;
            nodeIDZ.getElementsByClassName("InLnOrdWebPart_TransInfo")[0].replaceWith(nodeDiv);
            var node = document.getElementById(partNumber);
            var newButton = document.createElement("button");
            newButton.className = "button-add-to-order-inline add-to-order customButton";
            newButton.style = "font-size:11px;";
            var newSpan = document.createElement("span");
            newSpan.className = "button-reset--IE";
            newSpan.innerText = "add to assembly";
            newButton.appendChild(newSpan);
            newButton.onclick = function () { callAddToAssembly(); };
            node.appendChild(newButton);
        }
    });
});
mutationObserver.observe(document.documentElement,
    {
        characterData: true,
        childList: true,
        subtree: true,
        characterDataOldValue: true
    });
