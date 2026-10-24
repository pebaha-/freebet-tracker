document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("[data-autocomplete]").forEach(setupAutocomplete);
});

function setupAutocomplete(container) {
    const input = container.querySelector("[data-autocomplete-input]");
    const list = container.querySelector("[data-autocomplete-list]");
    const options = JSON.parse(container.dataset.autocompleteOptions || "[]");

    let filteredOptions = [];
    let activeIndex = -1;

    function closeList() {
        list.hidden = true;
        list.replaceChildren();
        input.setAttribute("aria-expanded", "false");
        input.removeAttribute("aria-activedescendant");
        activeIndex = -1;
    }

    function setActive(index) {
        activeIndex = index;

        const items = list.querySelectorAll("[role='option']");
        items.forEach((item, i) => {
            const active = i === activeIndex;
            item.classList.toggle("active", active);
            item.setAttribute("aria-selected", String(active));
        });

        if (activeIndex >= 0 && items[activeIndex]) {
            input.setAttribute("aria-activedescendant", items[activeIndex].id);
            items[activeIndex].scrollIntoView({ block: "nearest" });
        } else {
            input.removeAttribute("aria-activedescendant");
        }
    }

    function renderList() {
        const query = input.value.trim().toLocaleLowerCase();
        filteredOptions = options.filter(option =>
            option.toLocaleLowerCase().includes(query)
        );

        list.replaceChildren();
        activeIndex = -1;

        if (filteredOptions.length === 0) {
            closeList();
            return;
        }

        filteredOptions.forEach((option, index) => {
            const item = document.createElement("button");
            item.type = "button";
            item.className = "autocomplete-option";
            item.id = `${input.id}-option-${index}`;
            item.setAttribute("role", "option");
            item.setAttribute("aria-selected", "false");
            item.textContent = option;

            item.addEventListener("mousedown", event => {
                // Behold fokus på inputfeltet, indtil valget er gennemført.
                event.preventDefault();
                input.value = option;
                closeList();
                input.dispatchEvent(new Event("change", { bubbles: true }));
            });

            list.appendChild(item);
        });

        list.hidden = false;
        input.setAttribute("aria-expanded", "true");
    }

    input.addEventListener("input", renderList);

    input.addEventListener("focus", renderList);

    input.addEventListener("keydown", event => {
        if (list.hidden && (event.key === "ArrowDown" || event.key === "ArrowUp")) {
            renderList();
        }

        if (list.hidden) {
            return;
        }

        switch (event.key) {
            case "ArrowDown":
                event.preventDefault();
                setActive(Math.min(activeIndex + 1, filteredOptions.length - 1));
                break;

            case "ArrowUp":
                event.preventDefault();
                setActive(Math.max(activeIndex - 1, 0));
                break;

            case "Enter":
                if (activeIndex >= 0) {
                    event.preventDefault();
                    input.value = filteredOptions[activeIndex];
                    closeList();
                    input.dispatchEvent(new Event("change", { bubbles: true }));
                }
                break;

            case "Escape":
                closeList();
                break;
        }
    });

    document.addEventListener("click", event => {
        if (!container.contains(event.target)) {
            closeList();
        }
    });

    input.setAttribute("role", "combobox");
    input.setAttribute("aria-autocomplete", "list");
    input.setAttribute("aria-expanded", "false");
    input.setAttribute("aria-controls", list.id);
}
